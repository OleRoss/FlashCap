#if NET8_0_OR_GREATER
////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using FlashCap.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Media.MediaFoundation;
using Windows.Win32.System.Com;

namespace FlashCap.Internal;

internal static unsafe class MediaFoundationInterop
{
    internal const uint VideoStreamIndex = 0;

    internal readonly struct FormatKey : IEquatable<FormatKey>
    {
        internal FormatKey(uint mediaTypeIndex, Guid subtype, int width, int height, Fraction frameRate)
        {
            this.MediaTypeIndex = mediaTypeIndex;
            this.Subtype = subtype;
            this.Width = width;
            this.Height = height;
            this.FrameRate = frameRate;
        }

        internal uint MediaTypeIndex { get; }
        internal Guid Subtype { get; }
        internal int Width { get; }
        internal int Height { get; }
        internal Fraction FrameRate { get; }

        public bool Equals(FormatKey other) =>
            this.MediaTypeIndex == other.MediaTypeIndex &&
            this.Subtype == other.Subtype &&
            this.Width == other.Width &&
            this.Height == other.Height &&
            this.FrameRate == other.FrameRate;

        public override bool Equals(object? obj) => obj is FormatKey other && this.Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(this.MediaTypeIndex, this.Subtype, this.Width, this.Height, this.FrameRate);
    }

    internal readonly struct Format
    {
        internal Format(FormatKey key, VideoCharacteristics characteristics)
        {
            this.Key = key;
            this.Characteristics = characteristics;
        }

        internal FormatKey Key { get; }
        internal VideoCharacteristics Characteristics { get; }
    }

    internal readonly struct FrameLayout
    {
        internal FrameLayout(
            int rowLength,
            int rows,
            int sourceStride,
            int targetStride,
            bool bottomUp)
        {
            this.RowLength = rowLength;
            this.Rows = rows;
            this.SourceStride = sourceStride;
            this.TargetStride = targetStride;
            this.BottomUp = bottomUp;
        }

        internal int RowLength { get; }
        internal int Rows { get; }
        internal int SourceStride { get; }
        internal int TargetStride { get; }
        internal bool BottomUp { get; }
        internal int TargetLength => checked(this.TargetStride * this.Rows);
    }

    internal static void ThrowIfFailed(HRESULT result, string operation)
    {
        if (result.Failed)
        {
            throw new InvalidOperationException(
                $"FlashCap: {operation} failed (HRESULT=0x{unchecked((uint)result.Value):X8}).");
        }
    }

    internal static bool TryInitialize(out bool mediaFoundationStarted)
    {
        mediaFoundationStarted = false;
        var result = PInvoke.CoInitializeEx(COINIT.COINIT_MULTITHREADED);
        if (result.Failed)
        {
            Trace.WriteLine(
                $"FlashCap: CoInitializeEx failed (HRESULT=0x{unchecked((uint)result.Value):X8}).");
            return false;
        }

        result = PInvoke.MFStartup(PInvoke.MF_VERSION, PInvoke.MFSTARTUP_FULL);
        if (result.Failed)
        {
            Trace.WriteLine(
                $"FlashCap: MFStartup failed (HRESULT=0x{unchecked((uint)result.Value):X8}).");
            PInvoke.CoUninitialize();
            return false;
        }

        mediaFoundationStarted = true;
        return true;
    }

    internal static void Uninitialize(bool mediaFoundationStarted)
    {
        if (mediaFoundationStarted)
        {
            _ = PInvoke.MFShutdown();
            PInvoke.CoUninitialize();
        }
    }

    internal static IMFAttributes* CreateVideoCaptureAttributes()
    {
        IMFAttributes* attributes = null;
        ThrowIfFailed(PInvoke.MFCreateAttributes(&attributes, 1), nameof(PInvoke.MFCreateAttributes));
        try
        {
            ThrowIfFailed(
                attributes->SetGUID(
                    in PInvoke.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    in PInvoke.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID");
            return attributes;
        }
        catch
        {
            Release(attributes);
            throw;
        }
    }

    internal static IMFActivate** EnumerateDeviceSources(out uint count)
    {
        IMFAttributes* attributes = CreateVideoCaptureAttributes();
        try
        {
            ThrowIfFailed(
                PInvoke.MFEnumDeviceSources(attributes, out var devices, out count),
                nameof(PInvoke.MFEnumDeviceSources));
            return devices;
        }
        finally
        {
            Release(attributes);
        }
    }

    internal static string GetAllocatedString(IMFActivate* activate, in Guid key)
    {
        PWSTR value = default;
        var result = activate->GetAllocatedString(in key, out value, out _);
        if (result.Failed || value.Value is null)
        {
            return string.Empty;
        }

        try
        {
            return Marshal.PtrToStringUni((IntPtr)value.Value) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeCoTaskMem((IntPtr)value.Value);
        }
    }

    internal static IMFMediaSource* ActivateMediaSource(IMFActivate* activate)
    {
        void* value = null;
        ThrowIfFailed(
            activate->ActivateObject(in IMFMediaSource.IID_Guid, out value),
            "IMFActivate.ActivateObject");
        if (value is null)
        {
            throw new InvalidOperationException("FlashCap: Media Foundation returned no media source.");
        }
        return (IMFMediaSource*)value;
    }

    internal static IMFSourceReader* CreateSourceReader(IMFMediaSource* mediaSource)
    {
        IMFSourceReader* reader = null;
        ThrowIfFailed(
            PInvoke.MFCreateSourceReaderFromMediaSource(mediaSource, null, &reader),
            nameof(PInvoke.MFCreateSourceReaderFromMediaSource));
        if (reader is null)
        {
            throw new InvalidOperationException("FlashCap: Media Foundation returned no source reader.");
        }
        return reader;
    }

    internal static List<Format> EnumerateFormats(IMFSourceReader* reader)
    {
        var formats = new List<Format>();
        for (uint index = 0; ; index++)
        {
            IMFMediaType* mediaType = null;
            var result = reader->GetNativeMediaType(VideoStreamIndex, index, &mediaType);
            if (result == HRESULT.MF_E_NO_MORE_TYPES)
            {
                break;
            }
            ThrowIfFailed(result, "IMFSourceReader.GetNativeMediaType");
            try
            {
                if (mediaType is not null && TryCreateFormat(mediaType, index, out var format))
                {
                    formats.Add(format);
                }
            }
            finally
            {
                Release(mediaType);
            }
        }
        return formats;
    }

    internal static bool TryCreateFormat(IMFMediaType* mediaType, uint index, out Format format)
    {
        format = default;
        if (mediaType->GetGUID(in PInvoke.MF_MT_MAJOR_TYPE, out var majorType).Failed ||
            majorType != PInvoke.MFMediaType_Video ||
            mediaType->GetGUID(in PInvoke.MF_MT_SUBTYPE, out var subtype).Failed ||
            mediaType->GetUINT64(in PInvoke.MF_MT_FRAME_SIZE, out var frameSize).Failed ||
            mediaType->GetUINT64(in PInvoke.MF_MT_FRAME_RATE, out var frameRate).Failed)
        {
            return false;
        }

        var widthValue = (uint)(frameSize >> 32);
        var heightValue = (uint)frameSize;
        var numerator = (uint)(frameRate >> 32);
        var denominator = (uint)frameRate;
        if (widthValue is 0 or > int.MaxValue ||
            heightValue is 0 or > int.MaxValue ||
            numerator is 0 or > int.MaxValue ||
            denominator is 0 or > int.MaxValue ||
            !TryMapPixelFormat(subtype, out var pixelFormat, out var name))
        {
            return false;
        }

        var rate = new Fraction((int)numerator, (int)denominator);
        var characteristics = new VideoCharacteristics(
            pixelFormat, (int)widthValue, (int)heightValue, rate, name, true, name);
        format = new Format(
            new FormatKey(index, subtype, characteristics.Width, characteristics.Height, rate),
            characteristics);
        return true;
    }

    internal static bool TryMapPixelFormat(Guid subtype, out PixelFormats format, out string name)
    {
        if (subtype == PInvoke.MFVideoFormat_RGB24) { format = PixelFormats.RGB24; name = "RGB24"; return true; }
        if (subtype == PInvoke.MFVideoFormat_RGB32) { format = PixelFormats.RGB32; name = "RGB32"; return true; }
        if (subtype == PInvoke.MFVideoFormat_ARGB32) { format = PixelFormats.ARGB32; name = "ARGB32"; return true; }
        if (subtype == PInvoke.MFVideoFormat_RGB555) { format = PixelFormats.RGB15; name = "RGB555"; return true; }
        if (subtype == PInvoke.MFVideoFormat_RGB565) { format = PixelFormats.RGB16; name = "RGB565"; return true; }
        if (subtype == PInvoke.MFVideoFormat_MJPG) { format = PixelFormats.JPEG; name = "MJPG"; return true; }
        if (subtype == PInvoke.MFVideoFormat_UYVY) { format = PixelFormats.UYVY; name = "UYVY"; return true; }
        if (subtype == PInvoke.MFVideoFormat_YUY2) { format = PixelFormats.YUYV; name = "YUY2"; return true; }
        if (subtype == PInvoke.MFVideoFormat_NV12) { format = PixelFormats.NV12; name = "NV12"; return true; }
        format = PixelFormats.Unknown;
        name = subtype.ToString("D");
        return false;
    }

    internal static FrameLayout GetFrameLayout(
        PixelFormats format,
        int width,
        int height,
        int? defaultStride,
        int bufferLength)
    {
        var rowLength = format switch
        {
            PixelFormats.RGB24 => checked(width * 3),
            PixelFormats.RGB32 or PixelFormats.ARGB32 => checked(width * 4),
            PixelFormats.RGB15 or PixelFormats.RGB16 or PixelFormats.UYVY or PixelFormats.YUYV =>
                checked(width * 2),
            PixelFormats.NV12 => width,
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };
        var rows = checked(height + (format == PixelFormats.NV12 ? (height + 1) / 2 : 0));
        var rgb = format is PixelFormats.RGB15 or PixelFormats.RGB16 or
            PixelFormats.RGB24 or PixelFormats.RGB32 or PixelFormats.ARGB32;
        var bottomUp = defaultStride < 0 || defaultStride is null && rgb;
        var sourceStride = defaultStride is { } stride && stride != 0 ? Math.Abs(stride) :
            bufferLength % rows == 0 && bufferLength / rows >= rowLength ? bufferLength / rows : rowLength;
        var targetStride = rgb ? checked((rowLength + 3) & ~3) : rowLength;
        if (sourceStride < rowLength || (long)sourceStride * rows > bufferLength)
        {
            throw new ArgumentException("The frame buffer is truncated.", nameof(bufferLength));
        }
        return new FrameLayout(rowLength, rows, sourceStride, targetStride, bottomUp);
    }

    internal static void RepackFrame(
        ReadOnlySpan<byte> source,
        Span<byte> target,
        FrameLayout layout,
        bool reverseRows)
    {
        if ((long)layout.SourceStride * layout.Rows > source.Length || layout.TargetLength > target.Length)
        {
            throw new ArgumentException("The frame buffer is truncated.");
        }

        target[..layout.TargetLength].Clear();
        for (var row = 0; row < layout.Rows; row++)
        {
            var sourceRow = reverseRows ? layout.Rows - row - 1 : row;
            source.Slice(sourceRow * layout.SourceStride, layout.RowLength).
                CopyTo(target.Slice(row * layout.TargetStride, layout.RowLength));
        }
    }

    internal static IMFActivate* FindActivate(string symbolicLink)
    {
        IMFActivate** devices = null;
        uint count = 0;
        try
        {
            devices = EnumerateDeviceSources(out count);
            for (uint index = 0; index < count; index++)
            {
                var activate = devices[index];
                if (activate is not null && string.Equals(
                    GetAllocatedString(activate, in PInvoke.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK).Trim(),
                    symbolicLink, StringComparison.OrdinalIgnoreCase))
                {
                    devices[index] = null;
                    return activate;
                }
            }
        }
        finally
        {
            FreeActivateArray(devices, count);
        }
        throw new InvalidOperationException("FlashCap: The Media Foundation device is no longer available.");
    }

    internal static void FreeActivateArray(IMFActivate** devices, uint count)
    {
        if (devices is null)
        {
            return;
        }
        for (uint index = 0; index < count; index++)
        {
            Release(devices[index]);
        }
        Marshal.FreeCoTaskMem((IntPtr)devices);
    }

    internal static void Release<T>(T* value) where T : unmanaged
    {
        if (value is not null)
        {
            _ = ((Windows.Win32.System.Com.IUnknown*)value)->Release();
        }
    }

    internal static void TraceFailure(string operation, Exception exception) =>
        Trace.WriteLine($"FlashCap: Media Foundation {operation} failed: {exception}");
}
#endif
