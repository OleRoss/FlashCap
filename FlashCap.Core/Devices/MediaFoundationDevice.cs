////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using FlashCap.Internal;
using FlashCap.Internal.MediaFoundation;

namespace FlashCap.Devices;

[SupportedOSPlatform("windows")]
public sealed class MediaFoundationDevice : CaptureDevice
{
    private const int VideoStreamIndex = 0;

    private readonly TimestampCounter counter = new();
    private readonly string symbolicLink;

    private VideoCharacteristics requestedCharacteristics = null!;
    private TranscodeFormats transcodeFormat;
    private FrameProcessor frameProcessor = null!;
    private IMFSourceReader? sourceReader;
    private IntPtr sourceReaderPointer;
    private IntPtr mediaSourcePointer;
    private IntPtr bitmapHeader;
    private int sourceStride;
    private int targetStride;
    private int frameRowBytes;
    private bool sourceBottomUp;
    private bool targetBottomUp;
    private byte[]? repackBuffer;
    private Task? task;
    private CancellationTokenSource? stopping;
    private long frameIndex;

    internal MediaFoundationDevice(string symbolicLink, string name) :
        base(symbolicLink, name) =>
        this.symbolicLink = symbolicLink;

    private static unsafe IntPtr CreateBitmapHeader(
        VideoCharacteristics characteristics,
        MediaFoundationMediaTypes.FormatInfo formatInfo)
    {
        var pointer = NativeMethods.AllocateMemory((IntPtr)sizeof(NativeMethods.BITMAPINFOHEADER));
        try
        {
            var bitmapInfoHeader = (NativeMethods.BITMAPINFOHEADER*)pointer.ToPointer();
            bitmapInfoHeader->biSize = sizeof(NativeMethods.BITMAPINFOHEADER);
            bitmapInfoHeader->biCompression = formatInfo.Compression;
            bitmapInfoHeader->biPlanes = 1;
            bitmapInfoHeader->biBitCount = formatInfo.BitCount;
            bitmapInfoHeader->biWidth = characteristics.Width;
            bitmapInfoHeader->biHeight = characteristics.Height;
            bitmapInfoHeader->biSizeImage = bitmapInfoHeader->CalculateImageSize();
            return pointer;
        }
        catch
        {
            NativeMethods.FreeMemory(pointer);
            throw;
        }
    }

    private static bool IsRgbLike(PixelFormats pixelFormat) =>
        pixelFormat switch
        {
            PixelFormats.RGB8 => true,
            PixelFormats.RGB15 => true,
            PixelFormats.RGB16 => true,
            PixelFormats.RGB24 => true,
            PixelFormats.RGB32 => true,
            PixelFormats.ARGB32 => true,
            _ => false,
        };

    private static bool IsCompressed(PixelFormats pixelFormat) =>
        pixelFormat switch
        {
            PixelFormats.JPEG => true,
            PixelFormats.PNG => true,
            _ => false,
        };

    private static int AlignToDWord(int value) =>
        (value + 3) & ~3;

    private static int CalculateRowBytes(VideoCharacteristics characteristics) =>
        characteristics.PixelFormat switch
        {
            PixelFormats.RGB8 => characteristics.Width,
            PixelFormats.RGB15 => characteristics.Width * 2,
            PixelFormats.RGB16 => characteristics.Width * 2,
            PixelFormats.RGB24 => characteristics.Width * 3,
            PixelFormats.RGB32 => characteristics.Width * 4,
            PixelFormats.ARGB32 => characteristics.Width * 4,
            PixelFormats.UYVY => characteristics.Width * 2,
            PixelFormats.YUYV => characteristics.Width * 2,
            PixelFormats.NV12 => characteristics.Width,
            _ => 0,
        };

    private static int CalculateTargetStride(VideoCharacteristics characteristics) =>
        IsRgbLike(characteristics.PixelFormat) ?
            AlignToDWord(CalculateRowBytes(characteristics)) :
            CalculateRowBytes(characteristics);

    private static IntPtr FindActivate(string symbolicLink)
    {
        foreach (var activatePointer in MediaFoundationDevices.EnumerateDeviceActivates())
        {
            var release = true;
            try
            {
                var activate = MediaFoundationCom.Wrap<IMFActivate>(activatePointer);
                var currentSymbolicLink = MediaFoundationCom.GetAllocatedString(
                    activate,
                    in NativeMethods_MediaFoundation.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK).Trim();
                if (string.Equals(currentSymbolicLink, symbolicLink, StringComparison.OrdinalIgnoreCase))
                {
                    release = false;
                    return activatePointer;
                }
            }
            finally
            {
                if (release)
                {
                    MediaFoundationCom.Release(activatePointer);
                }
            }
        }

        return IntPtr.Zero;
    }

    private static IntPtr FindMediaType(
        IMFSourceReader sourceReader,
        VideoCharacteristics characteristics,
        out MediaFoundationMediaTypes.FormatInfo formatInfo)
    {
        for (var index = 0; ; index++)
        {
            var hr = sourceReader.GetNativeMediaType(
                VideoStreamIndex,
                index,
                out var mediaTypePointer);
            if (hr == NativeMethods_MediaFoundation.MF_E_NO_MORE_TYPES)
            {
                break;
            }
            if (hr < 0)
            {
                break;
            }
            if (mediaTypePointer == IntPtr.Zero)
            {
                continue;
            }

            var release = true;
            try
            {
                var mediaType = MediaFoundationCom.Wrap<IMFMediaType>(mediaTypePointer);
                if (MediaFoundationMediaTypes.TryCreateVideoCharacteristics(
                    mediaType,
                    out var currentCharacteristics,
                    out formatInfo) &&
                    characteristics.Equals(currentCharacteristics))
                {
                    release = false;
                    return mediaTypePointer;
                }
            }
            finally
            {
                if (release)
                {
                    MediaFoundationCom.Release(mediaTypePointer);
                }
            }
        }

        formatInfo = default;
        return IntPtr.Zero;
    }

    protected override Task OnInitializeAsync(
        VideoCharacteristics characteristics,
        TranscodeFormats transcodeFormat,
        FrameProcessor frameProcessor,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        MediaFoundationSession.EnsureStarted();

        this.Characteristics = characteristics;
        this.requestedCharacteristics = characteristics;
        this.transcodeFormat = transcodeFormat;
        this.frameProcessor = frameProcessor;

        return TaskCompat.CompletedTask;
    }

    private void InitializeMediaFoundationObjects()
    {
        MediaFoundationSession.EnsureStarted();

        var activatePointer = FindActivate(this.symbolicLink);
        if (activatePointer == IntPtr.Zero)
        {
            throw new ArgumentException(
                $"FlashCap: Couldn't find a Media Foundation device: SymbolicLink={this.symbolicLink}");
        }

        try
        {
            var activate = MediaFoundationCom.Wrap<IMFActivate>(activatePointer);
            NativeMethods_MediaFoundation.ThrowIfFailed(
                activate.ActivateObject(
                    in NativeMethods_MediaFoundation.IID_IMFMediaSource,
                    out this.mediaSourcePointer),
                "IMFActivate.ActivateObject(IMFMediaSource)");

            NativeMethods_MediaFoundation.ThrowIfFailed(
                NativeMethods_MediaFoundation.MFCreateSourceReaderFromMediaSource(
                    this.mediaSourcePointer,
                    IntPtr.Zero,
                    out this.sourceReaderPointer),
                nameof(NativeMethods_MediaFoundation.MFCreateSourceReaderFromMediaSource));

            this.sourceReader = MediaFoundationCom.Wrap<IMFSourceReader>(this.sourceReaderPointer);

            this.sourceReader.SetStreamSelection(
                NativeMethods_MediaFoundation.MF_SOURCE_READER_ALL_STREAMS,
                0);
            NativeMethods_MediaFoundation.ThrowIfFailed(
                this.sourceReader.SetStreamSelection(
                    VideoStreamIndex,
                    1),
                "IMFSourceReader.SetStreamSelection(video stream)");

            var mediaTypePointer = FindMediaType(
                this.sourceReader,
                this.requestedCharacteristics,
                out var formatInfo);
            if (mediaTypePointer == IntPtr.Zero)
            {
                throw new ArgumentException(
                    $"FlashCap: Couldn't set Media Foundation video format: SymbolicLink={this.symbolicLink}");
            }

            try
            {
                NativeMethods_MediaFoundation.ThrowIfFailed(
                    this.sourceReader.SetCurrentMediaType(
                        VideoStreamIndex,
                        IntPtr.Zero,
                        mediaTypePointer),
                    "IMFSourceReader.SetCurrentMediaType");

                this.ConfigureFrameLayout(mediaTypePointer, formatInfo);
                this.bitmapHeader = CreateBitmapHeader(this.requestedCharacteristics, formatInfo);
            }
            finally
            {
                MediaFoundationCom.Release(mediaTypePointer);
            }
        }
        catch
        {
            this.ReleaseMediaFoundationObjects();
            throw;
        }
        finally
        {
            MediaFoundationCom.Release(activatePointer);
        }
    }

    private void ConfigureFrameLayout(
        IntPtr mediaTypePointer,
        MediaFoundationMediaTypes.FormatInfo formatInfo)
    {
        var mediaType = MediaFoundationCom.Wrap<IMFMediaType>(mediaTypePointer);

        this.frameRowBytes = CalculateRowBytes(this.requestedCharacteristics);
        this.targetStride = CalculateTargetStride(this.requestedCharacteristics);
        this.targetBottomUp = IsRgbLike(formatInfo.PixelFormat);

        if (!IsCompressed(formatInfo.PixelFormat) &&
            mediaType.GetUINT32(
                in NativeMethods_MediaFoundation.MF_MT_DEFAULT_STRIDE,
                out var defaultStride) >= 0 &&
            defaultStride != 0)
        {
            this.sourceBottomUp = defaultStride < 0;
            this.sourceStride = Math.Abs(defaultStride);
        }
        else
        {
            this.sourceBottomUp = false;
            this.sourceStride = this.targetStride;
        }
    }

    protected override async Task OnStartAsync(CancellationToken ct)
    {
        if (this.IsRunning)
        {
            return;
        }

        this.frameIndex = 0;
        this.counter.Restart();
        this.stopping = CancellationTokenSource.CreateLinkedTokenSource(ct);
        this.IsRunning = true;
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        this.task = Task.Factory.StartNew(
            () => this.ThreadEntry(started),
            this.stopping.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);

        try
        {
            await started.Task.WaitAsync(ct).
                ConfigureAwait(false);
        }
        catch
        {
            await this.OnStopAsync(default).
                ConfigureAwait(false);
            throw;
        }
    }

    protected override async Task OnStopAsync(CancellationToken ct)
    {
        if (!this.IsRunning && this.task == null)
        {
            return;
        }

        this.IsRunning = false;
        var stopping = Interlocked.Exchange(ref this.stopping, null);
        var task = Interlocked.Exchange(ref this.task, null);

        stopping?.Cancel();
        try
        {
            this.sourceReader?.Flush(NativeMethods_MediaFoundation.MF_SOURCE_READER_ALL_STREAMS);
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
        }

        if (task != null)
        {
            await task.ConfigureAwait(false);
        }

        stopping?.Dispose();
    }

    private void ThreadEntry(TaskCompletionSource started)
    {
        var initializedCom = false;
        var coInitializeHr = NativeMethods.CoInitializeEx(IntPtr.Zero, NativeMethods.COINIT.MULTITHREADED);
        if (coInitializeHr >= 0)
        {
            initializedCom = true;
        }

        try
        {
            this.InitializeMediaFoundationObjects();
            started.TrySetResult();

            var sourceReader = this.sourceReader;
            if (sourceReader == null)
            {
                return;
            }

            while (this.IsRunning)
            {
                var hr = sourceReader.ReadSample(
                    NativeMethods_MediaFoundation.MF_SOURCE_READER_ANY_STREAM,
                    0,
                    out _,
                    out var streamFlags,
                    out var timestamp,
                    out var samplePointer);
                if (hr < 0)
                {
                    Trace.WriteLine($"FlashCap: IMFSourceReader.ReadSample failed: HR=0x{hr:x8}");
                    break;
                }

                if ((streamFlags & NativeMethods_MediaFoundation.MF_SOURCE_READERF_ENDOFSTREAM) != 0)
                {
                    break;
                }

                if ((streamFlags &
                    (NativeMethods_MediaFoundation.MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED |
                     NativeMethods_MediaFoundation.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED |
                     NativeMethods_MediaFoundation.MF_SOURCE_READERF_STREAMTICK)) != 0)
                {
                    MediaFoundationCom.Release(samplePointer);
                    continue;
                }

                if (samplePointer == IntPtr.Zero)
                {
                    continue;
                }

                this.ProcessSample(samplePointer, timestamp);
            }
        }
        catch (Exception ex)
        {
            started.TrySetException(ex);
            Trace.WriteLine(ex);
        }
        finally
        {
            this.IsRunning = false;
            this.ReleaseMediaFoundationObjects();

            if (initializedCom)
            {
                NativeMethods.CoUninitialize();
            }
        }
    }

    private unsafe void ProcessSample(IntPtr samplePointer, long timestamp)
    {
        var bufferPointer = IntPtr.Zero;
        try
        {
            var sample = MediaFoundationCom.Wrap<IMFSample>(samplePointer);
            if (sample.ConvertToContiguousBuffer(out bufferPointer) < 0 ||
                bufferPointer == IntPtr.Zero)
            {
                return;
            }

            var buffer = MediaFoundationCom.Wrap<IMFMediaBuffer>(bufferPointer);
            if (buffer.Lock(out var data, out _, out var currentLength) < 0)
            {
                return;
            }

            try
            {
                if (!this.TryRepackFrame(
                    data,
                    currentLength,
                    out var repacked,
                    out var frameLength))
                {
                    return;
                }

                if (repacked is { })
                {
                    fixed (byte* repackedData = repacked)
                    {
                        this.frameProcessor.OnFrameArrived(
                            this,
                            (IntPtr)repackedData,
                            frameLength,
                            timestamp > 0 ? timestamp / 10 : this.counter.ElapsedMicroseconds,
                            this.frameIndex++);
                    }
                }
                else
                {
                    this.frameProcessor.OnFrameArrived(
                        this,
                        data,
                        frameLength,
                        timestamp > 0 ? timestamp / 10 : this.counter.ElapsedMicroseconds,
                        this.frameIndex++);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
            finally
            {
                buffer.Unlock();
            }
        }
        finally
        {
            MediaFoundationCom.Release(bufferPointer);
            MediaFoundationCom.Release(samplePointer);
        }
    }

    private unsafe bool TryRepackFrame(
        IntPtr data,
        int currentLength,
        out byte[]? repacked,
        out int frameLength)
    {
        repacked = null;
        frameLength = 0;

        if (IsCompressed(this.requestedCharacteristics.PixelFormat))
        {
            frameLength = currentLength;
            return currentLength >= 1;
        }

        var width = this.requestedCharacteristics.Width;
        var height = this.requestedCharacteristics.Height;
        if (width <= 0 || height <= 0 ||
            this.sourceStride <= 0 || this.targetStride <= 0 || this.frameRowBytes <= 0)
        {
            return false;
        }

        var uvRows = this.requestedCharacteristics.PixelFormat == PixelFormats.NV12 ?
            (height + 1) / 2 :
            0;
        var sourceStride = this.sourceStride;
        var sourceBottomUp = this.sourceBottomUp;
        var sourceRows = height + uvRows;
        var sourceLength = sourceStride * sourceRows;
        if (currentLength < sourceLength &&
            currentLength >= this.frameRowBytes * sourceRows &&
            currentLength % sourceRows == 0)
        {
            sourceStride = currentLength / sourceRows;
            sourceBottomUp = false;
            sourceLength = currentLength;
        }

        if (sourceStride < this.frameRowBytes || this.targetStride < this.frameRowBytes)
        {
            return false;
        }

        var targetLength = this.targetStride * (height + uvRows);
        if (currentLength < sourceLength)
        {
            return false;
        }

        frameLength = targetLength;
        if (sourceStride == this.targetStride &&
            sourceBottomUp == this.targetBottomUp)
        {
            return true;
        }

        if (this.repackBuffer == null || this.repackBuffer.Length < targetLength)
        {
            this.repackBuffer = new byte[targetLength];
        }

        repacked = this.repackBuffer;
        Array.Clear(repacked, 0, targetLength);

        fixed (byte* target = repacked)
        {
            var source = (byte*)data.ToPointer();
            if (this.requestedCharacteristics.PixelFormat == PixelFormats.NV12)
            {
                this.CopyRows(
                    source,
                    target,
                    height,
                    sourceStride,
                    sourceBottomUp,
                    this.frameRowBytes,
                    false);
                this.CopyRows(
                    source + sourceStride * height,
                    target + this.targetStride * height,
                    uvRows,
                    sourceStride,
                    sourceBottomUp,
                    this.frameRowBytes,
                    false);
            }
            else
            {
                this.CopyRows(
                    source,
                    target,
                    height,
                    sourceStride,
                    sourceBottomUp,
                    this.frameRowBytes,
                    this.targetBottomUp);
            }
        }

        return true;
    }

    private unsafe void CopyRows(
        byte* source,
        byte* target,
        int rows,
        int sourceStride,
        bool sourceBottomUp,
        int rowBytes,
        bool targetBottomUp)
    {
        for (var row = 0; row < rows; row++)
        {
            var sourceRow = sourceBottomUp ? rows - row - 1 : row;
            var targetRow = targetBottomUp ? rows - row - 1 : row;

            Buffer.MemoryCopy(
                source + sourceRow * sourceStride,
                target + targetRow * this.targetStride,
                this.targetStride,
                rowBytes);
        }
    }

    protected override async Task OnDisposeAsync()
    {
        await this.OnStopAsync(default).
            ConfigureAwait(false);

        if (this.frameProcessor != null)
        {
            await this.frameProcessor.DisposeAsync().
                ConfigureAwait(false);
            this.frameProcessor = null!;
        }

        this.ReleaseMediaFoundationObjects();
    }

    private void ReleaseMediaFoundationObjects()
    {
        this.sourceReader = null;

        MediaFoundationCom.Release(this.sourceReaderPointer);
        this.sourceReaderPointer = IntPtr.Zero;

        if (this.mediaSourcePointer != IntPtr.Zero)
        {
            try
            {
                MediaFoundationCom.Wrap<IMFMediaSource>(this.mediaSourcePointer).Shutdown();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }

            MediaFoundationCom.Release(this.mediaSourcePointer);
            this.mediaSourcePointer = IntPtr.Zero;
        }

        if (this.bitmapHeader != IntPtr.Zero)
        {
            NativeMethods.FreeMemory(this.bitmapHeader);
            this.bitmapHeader = IntPtr.Zero;
        }
    }

#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
    protected override void OnCapture(
        IntPtr pData,
        int size,
        long timestampMicroseconds,
        long frameIndex,
        PixelBuffer buffer) =>
        buffer.CopyIn(
            this.bitmapHeader,
            pData,
            size,
            timestampMicroseconds,
            frameIndex,
            this.transcodeFormat);
}
