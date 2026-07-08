////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FlashCap.Internal;
using FlashCap.Internal.MediaFoundation;

namespace FlashCap.Devices;

[SupportedOSPlatform("windows")]
public sealed class MediaFoundationDevices : CaptureDevices
{
    public MediaFoundationDevices() :
        this(new DefaultBufferPool())
    {
    }

    public MediaFoundationDevices(BufferPool defaultBufferPool) :
        base(defaultBufferPool)
    {
    }

    private static IntPtr CreateVideoCaptureAttributes()
    {
        NativeMethods_MediaFoundation.ThrowIfFailed(
            NativeMethods_MediaFoundation.MFCreateAttributes(out var attributesPointer, 1),
            nameof(NativeMethods_MediaFoundation.MFCreateAttributes));

        try
        {
            var attributes = MediaFoundationCom.Wrap<IMFAttributes>(attributesPointer);
            NativeMethods_MediaFoundation.ThrowIfFailed(
                attributes.SetGUID(
                    in NativeMethods_MediaFoundation.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    in NativeMethods_MediaFoundation.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID),
                "IMFAttributes.SetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            NativeMethods_MediaFoundation.ThrowIfFailed(
                attributes.GetGUID(
                    in NativeMethods_MediaFoundation.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE,
                    out var sourceType),
                "IMFAttributes.GetGUID(MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE)");
            if (sourceType != NativeMethods_MediaFoundation.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID)
            {
                throw new InvalidOperationException(
                    "FlashCap: Media Foundation device source attribute verification failed.");
            }
            return attributesPointer;
        }
        catch
        {
            MediaFoundationCom.Release(attributesPointer);
            throw;
        }
    }

    internal static IEnumerable<IntPtr> EnumerateDeviceActivates()
    {
        MediaFoundationSession.EnsureStarted();

        var attributesPointer = CreateVideoCaptureAttributes();
        try
        {
            NativeMethods_MediaFoundation.ThrowIfFailed(
                NativeMethods_MediaFoundation.MFEnumDeviceSources(
                    attributesPointer,
                    out var activateArray,
                    out var count),
                nameof(NativeMethods_MediaFoundation.MFEnumDeviceSources));

            try
            {
                for (var index = 0; index < count; index++)
                {
                    var activatePointer = Marshal.ReadIntPtr(activateArray, index * IntPtr.Size);
                    if (activatePointer != IntPtr.Zero)
                    {
                        yield return activatePointer;
                    }
                }
            }
            finally
            {
                Marshal.FreeCoTaskMem(activateArray);
            }
        }
        finally
        {
            MediaFoundationCom.Release(attributesPointer);
        }
    }

    private static VideoCharacteristics[] EnumerateCharacteristics(IMFActivate activate)
    {
        var mediaSourcePointer = IntPtr.Zero;
        var sourceReaderPointer = IntPtr.Zero;

        try
        {
            var hr = activate.ActivateObject(
                in NativeMethods_MediaFoundation.IID_IMFMediaSource,
                out mediaSourcePointer);
            if (hr < 0 || mediaSourcePointer == IntPtr.Zero)
            {
                return ArrayEx.Empty<VideoCharacteristics>();
            }

            hr = NativeMethods_MediaFoundation.MFCreateSourceReaderFromMediaSource(
                mediaSourcePointer,
                IntPtr.Zero,
                out sourceReaderPointer);
            if (hr < 0 || sourceReaderPointer == IntPtr.Zero)
            {
                return ArrayEx.Empty<VideoCharacteristics>();
            }

            var sourceReader = MediaFoundationCom.Wrap<IMFSourceReader>(sourceReaderPointer);
            return EnumerateCharacteristics(sourceReader).
                Distinct().
                OrderByDescending(vc => vc).
                ToArray();
        }
        catch (Exception ex)
        {
            Trace.WriteLine(ex);
            return ArrayEx.Empty<VideoCharacteristics>();
        }
        finally
        {
            MediaFoundationCom.Release(sourceReaderPointer);

            if (mediaSourcePointer != IntPtr.Zero)
            {
                try
                {
                    MediaFoundationCom.Wrap<IMFMediaSource>(mediaSourcePointer).Shutdown();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
                MediaFoundationCom.Release(mediaSourcePointer);
            }

            try
            {
                activate.ShutdownObject();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }
    }

    internal static IEnumerable<VideoCharacteristics> EnumerateCharacteristics(IMFSourceReader sourceReader)
    {
        for (var index = 0; ; index++)
        {
            var hr = sourceReader.GetNativeMediaType(
                0,
                index,
                out var mediaTypePointer);
            if (hr == NativeMethods_MediaFoundation.MF_E_NO_MORE_TYPES)
            {
                yield break;
            }
            if (hr < 0)
            {
                yield break;
            }
            if (mediaTypePointer == IntPtr.Zero)
            {
                continue;
            }

            try
            {
                var mediaType = MediaFoundationCom.Wrap<IMFMediaType>(mediaTypePointer);
                if (MediaFoundationMediaTypes.TryCreateVideoCharacteristics(
                    mediaType,
                    out var characteristics,
                    out _))
                {
                    yield return characteristics;
                }
            }
            finally
            {
                MediaFoundationCom.Release(mediaTypePointer);
            }
        }
    }

    protected override IEnumerable<CaptureDeviceDescriptor> OnEnumerateDescriptors()
    {
        if (!OperatingSystem.IsWindows())
            throw new UnreachableException();

        var descriptors = new List<CaptureDeviceDescriptor>();
        foreach (var activatePointer in EnumerateDeviceActivates())
        {
            try
            {
                var activate = MediaFoundationCom.Wrap<IMFActivate>(activatePointer);
                var name = MediaFoundationCom.GetAllocatedString(
                    activate,
                    in NativeMethods_MediaFoundation.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME).Trim();
                var symbolicLink = MediaFoundationCom.GetAllocatedString(
                    activate,
                    in NativeMethods_MediaFoundation.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK).Trim();

                if (string.IsNullOrEmpty(symbolicLink))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(name))
                {
                    name = "Media Foundation camera";
                }

                descriptors.Add(new MediaFoundationDeviceDescriptor(
                    symbolicLink,
                    name,
                    $"{name} (Media Foundation)",
                    EnumerateCharacteristics(activate),
                    this.DefaultBufferPool));
            }
            finally
            {
                MediaFoundationCom.Release(activatePointer);
            }
        }

        return descriptors;
    }
}

[SupportedOSPlatform("windows")]
internal static class MediaFoundationSession
{
    private static readonly object SyncLock = new();
    private static bool started;

    public static void EnsureStarted()
    {
        lock (SyncLock)
        {
            if (!started)
            {
                NativeMethods_MediaFoundation.ThrowIfFailed(
                    NativeMethods_MediaFoundation.MFStartup(
                        NativeMethods_MediaFoundation.MF_VERSION,
                        NativeMethods_MediaFoundation.MFSTARTUP_FULL),
                    nameof(NativeMethods_MediaFoundation.MFStartup));
                started = true;
            }
        }
    }
}
