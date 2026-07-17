#if FLASHCAP_MEDIAFOUNDATION
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
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using FlashCap.Internal.MediaFoundation;

namespace FlashCap.Devices;

[SupportedOSPlatform("windows6.0")]
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
            throw new PlatformNotSupportedException();
        }

        try
        {
            return Task.Factory.StartNew(
                () => MediaFoundationInterop.EnumerateDevices()
                    .Select(device => (CaptureDeviceDescriptor)new MediaFoundationDeviceDescriptor(
                        device.SymbolicLink,
                        device.Name,
                        $"{device.Name} (Media Foundation)",
                        device.Formats,
                        this.DefaultBufferPool))
                    .ToArray(),
                CancellationToken.None,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach,
                TaskScheduler.Default).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            MediaFoundationHelpers.TraceFailure("device discovery", exception);
            return [];
        }
    }

}
#endif
