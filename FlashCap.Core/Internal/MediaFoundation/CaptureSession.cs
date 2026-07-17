#if NET8_0_OR_GREATER
////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////
using System;
using System.Threading;
using Windows.Win32;
using Windows.Win32.Media.MediaFoundation;
using static FlashCap.Internal.MediaFoundation.MediaFoundationInterop;

namespace FlashCap.Internal.MediaFoundation;

internal sealed unsafe class CaptureSession : IDisposable
{
    private IMFActivate* activate;
    private IMFMediaSource* mediaSource;
    private IMFSourceReader* reader;
    private int? defaultStride;

    private CaptureSession()
    {
    }

    internal IMFSourceReader* SourceReader => this.reader;

    internal static CaptureSession Open(string symbolicLink, FormatKey formatKey)
    {
        var session = new CaptureSession();
        try
        {
            session.activate = FindActivate(symbolicLink);
            session.mediaSource = ActivateMediaSource(session.activate);
            session.reader = CreateSourceReader(session.mediaSource);
            session.defaultStride = ConfigureReader(session.reader, formatKey);
            return session;
        }
        catch
        {
            session.Dispose();
            throw;
        }
    }

    internal void ReadFrames(CancellationToken stopToken, FrameHandler frameHandler)
    {
        long? firstTimestamp = null;
        long frameIndex = 0;
        while (!stopToken.IsCancellationRequested)
        {
            uint flags = 0;
            long timestamp = 0;
            IMFSample* sample = null;
            var result = this.reader->ReadSample(
                VideoStreamIndex,
                0,
                null,
                &flags,
                &timestamp,
                &sample);
            if (result.Failed)
            {
                if (stopToken.IsCancellationRequested)
                {
                    break;
                }
                MediaFoundationHelpers.ThrowIfFailed(result, "IMFSourceReader.ReadSample");
            }

            try
            {
                if (stopToken.IsCancellationRequested)
                {
                    break;
                }
                var readerFlags = (MF_SOURCE_READER_FLAG)flags;
                if ((readerFlags & (MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ERROR |
                                    MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_ENDOFSTREAM |
                                    MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED |
                                    MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED)) != 0)
                {
                    throw new InvalidOperationException(
                        $"FlashCap: Media Foundation capture stream changed state ({readerFlags}).");
                }
                if (sample is null || (readerFlags & MF_SOURCE_READER_FLAG.MF_SOURCE_READERF_STREAMTICK) != 0)
                {
                    continue;
                }

                firstTimestamp ??= timestamp;
                ProcessSample(
                    sample,
                    this.defaultStride,
                    Math.Max(0, timestamp - firstTimestamp.Value) / 10,
                    frameIndex++,
                    frameHandler);
            }
            finally
            {
                Release(sample);
            }
        }
    }

    private static int? ConfigureReader(IMFSourceReader* reader, FormatKey formatKey)
    {
        MediaFoundationHelpers.ThrowIfFailed(
            reader->SetStreamSelection(unchecked((uint)MF_SOURCE_READER_CONSTANTS.MF_SOURCE_READER_ALL_STREAMS), false),
            "IMFSourceReader.SetStreamSelection(all)");
        MediaFoundationHelpers.ThrowIfFailed(
            reader->SetStreamSelection(VideoStreamIndex, true),
            "IMFSourceReader.SetStreamSelection(video)");

        IMFMediaType* mediaType = null;
        try
        {
            MediaFoundationHelpers.ThrowIfFailed(
                reader->GetNativeMediaType(VideoStreamIndex, formatKey.MediaTypeIndex, &mediaType),
                "IMFSourceReader.GetNativeMediaType");
            if (mediaType is null ||
                !TryCreateFormat(mediaType, formatKey.MediaTypeIndex, out var selected) ||
                selected.Key != formatKey)
            {
                throw new InvalidOperationException(
                    "FlashCap: The selected Media Foundation format is no longer available.");
            }

            MediaFoundationHelpers.ThrowIfFailed(
                reader->SetCurrentMediaType(VideoStreamIndex, mediaType),
                "IMFSourceReader.SetCurrentMediaType");
            return mediaType->GetUINT32(in PInvoke.MF_MT_DEFAULT_STRIDE, out var stride).Succeeded ?
                unchecked((int)stride) : null;
        }
        finally
        {
            Release(mediaType);
        }
    }

    private static void ProcessSample(
        IMFSample* sample,
        int? defaultStride,
        long timestampMicroseconds,
        long frameIndex,
        FrameHandler frameHandler)
    {
        IMFMediaBuffer* buffer = null;
        MediaFoundationHelpers.ThrowIfFailed(
            sample->ConvertToContiguousBuffer(&buffer),
            "IMFSample.ConvertToContiguousBuffer");
        if (buffer is null)
        {
            throw new InvalidOperationException("FlashCap: Media Foundation returned no sample buffer.");
        }

        byte* data = null;
        bool locked = false;
        try
        {
            uint currentLength = 0;
            MediaFoundationHelpers.ThrowIfFailed(buffer->Lock(&data, null, &currentLength), "IMFMediaBuffer.Lock");
            locked = true;
            if (data is null || currentLength == 0 || currentLength > int.MaxValue)
            {
                throw new InvalidOperationException(
                    "FlashCap: Media Foundation returned an invalid frame buffer.");
            }

            frameHandler(
                data,
                checked((int)currentLength),
                defaultStride,
                timestampMicroseconds,
                frameIndex);
        }
        finally
        {
            if (locked)
            {
                _ = buffer->Unlock();
            }
            Release(buffer);
        }
    }

    public void Dispose()
    {
        if (this.reader is null && this.mediaSource is not null)
        {
            _ = this.mediaSource->Shutdown();
        }
        Release(this.reader);
        this.reader = null;
        if (this.activate is not null)
        {
            _ = this.activate->ShutdownObject();
        }
        Release(this.mediaSource);
        this.mediaSource = null;
        Release(this.activate);
        this.activate = null;
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
}
#endif