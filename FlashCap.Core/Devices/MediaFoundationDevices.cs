#if NET8_0_OR_GREATER
////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using FlashCap.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Media.MediaFoundation;

namespace FlashCap.Devices;

public sealed class MediaFoundationDevices(BufferPool defaultBufferPool) : CaptureDevices(defaultBufferPool)
{
    public MediaFoundationDevices() :
        this(new DefaultBufferPool())
    {
    }

    protected override IEnumerable<CaptureDeviceDescriptor> OnEnumerateDescriptors()
    {
        if (!OperatingSystem.IsWindows())
        {
            return Array.Empty<CaptureDeviceDescriptor>();
        }

        try
        {
            return Task.Factory.StartNew(
                this.EnumerateDescriptors,
                CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            MediaFoundationInterop.TraceFailure("device discovery", exception);
            return Array.Empty<CaptureDeviceDescriptor>();
        }
    }

    private unsafe CaptureDeviceDescriptor[] EnumerateDescriptors()
    {
        if (!MediaFoundationInterop.TryInitialize(out var started))
        {
            return Array.Empty<CaptureDeviceDescriptor>();
        }

        IMFActivate** devices = null;
        uint count = 0;
        try
        {
            devices = MediaFoundationInterop.EnumerateDeviceSources(out count);
            var descriptors = new List<CaptureDeviceDescriptor>(checked((int)count));
            for (uint index = 0; index < count; index++)
            {
                var activate = devices[index];
                devices[index] = null;
                if (activate is null)
                {
                    continue;
                }

                try
                {
                    var symbolicLink = MediaFoundationInterop.GetAllocatedString(
                        activate,
                        in PInvoke.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK).Trim();
                    if (string.IsNullOrEmpty(symbolicLink))
                    {
                        continue;
                    }

                    var name = MediaFoundationInterop.GetAllocatedString(
                        activate,
                        in PInvoke.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME).Trim();
                    if (string.IsNullOrEmpty(name))
                    {
                        name = "Media Foundation camera";
                    }

                    var formats = EnumerateFormats(activate);
                    if (formats.Count == 0)
                    {
                        continue;
                    }

                    descriptors.Add(new MediaFoundationDeviceDescriptor(
                        symbolicLink,
                        name,
                        $"{name} (Media Foundation)",
                        formats,
                        this.DefaultBufferPool));
                }
                catch (Exception exception)
                {
                    MediaFoundationInterop.TraceFailure("device inspection", exception);
                }
                finally
                {
                    _ = activate->ShutdownObject();
                    MediaFoundationInterop.Release(activate);
                }
            }
            return descriptors.ToArray();
        }
        finally
        {
            MediaFoundationInterop.FreeActivateArray(devices, count);
            MediaFoundationInterop.Uninitialize(started);
        }
    }

    private static unsafe Dictionary<VideoCharacteristics, MediaFoundationInterop.FormatKey> EnumerateFormats(
        IMFActivate* activate)
    {
        IMFMediaSource* mediaSource = null;
        IMFSourceReader* reader = null;
        try
        {
            mediaSource = MediaFoundationInterop.ActivateMediaSource(activate);
            reader = MediaFoundationInterop.CreateSourceReader(mediaSource);
            return MediaFoundationInterop.EnumerateFormats(reader)
                .DistinctBy(format => format.Characteristics)
                .ToDictionary(format => format.Characteristics, format => format.Key);
        }
        finally
        {
            MediaFoundationInterop.Release(reader);
            if (reader is null && mediaSource is not null)
            {
                _ = mediaSource->Shutdown();
            }
            MediaFoundationInterop.Release(mediaSource);
        }
    }
}
#endif
