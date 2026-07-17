////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
// Copyright (c) Yoh Deadfall (@YohDeadfall)
// Copyright (c) Felipe Ferreira Quintella (@ffquintella)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using FlashCap.Devices;
using FlashCap.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FlashCap;

/// <summary>
/// By default, the following backends are considered for <see cref="OnEnumerateDescriptors"/>:
/// <list type= "bullet">
/// <item><description><see cref="DirectShowDevices"/> (Win)- Only if <c>RuntimeFeature.IsDynamicCodeSupported</c> is true</description></item>
/// <item><description><see cref="VideoForWindowsDevices"/> (Win)</description></item>
/// <item><description><c>MediaFoundationDevices</c> (Win) - Supported on .NET 5.0 or greater and Windows 6.0 or greater</description></item>
/// <item><description><see cref="V4L2Devices"/> (Linux)</description></item>
/// <item><description><see cref="AVFoundationDevices"/> (MacOs)</description></item>
/// </list>
/// </summary>
public class CaptureDevices
{
    protected readonly BufferPool DefaultBufferPool;

    public CaptureDevices() :
        this(new DefaultBufferPool())
    {
    }

    public CaptureDevices(BufferPool defaultBufferPool)
    {
        DefaultBufferPool = defaultBufferPool;
    }

    protected virtual IEnumerable<CaptureDeviceDescriptor> OnEnumerateDescriptors()
    {
        switch (NativeMethods.CurrentPlatform)
        {
            case NativeMethods.Platforms.Windows:
            {
                IEnumerable<CaptureDeviceDescriptor> descriptors = [];
#if NETSTANDARD2_1 || NETCOREAPP3_0_OR_GREATER
                if (RuntimeFeature.IsDynamicCodeSupported)
#endif
                    descriptors = new DirectShowDevices(this.DefaultBufferPool).OnEnumerateDescriptors();
                descriptors = descriptors.Concat(new VideoForWindowsDevices(this.DefaultBufferPool).OnEnumerateDescriptors());
#if FLASHCAP_MEDIAFOUNDATION
                descriptors = descriptors.Concat(new MediaFoundationDevices(this.DefaultBufferPool).OnEnumerateDescriptors());
#endif
                return descriptors;
            }
            case NativeMethods.Platforms.Linux:
                return new V4L2Devices().OnEnumerateDescriptors();
            case NativeMethods.Platforms.MacOS:
                return new AVFoundationDevices().OnEnumerateDescriptors();
            default:
                return ArrayEx.Empty<CaptureDeviceDescriptor>();
        }
    }

    internal IEnumerable<CaptureDeviceDescriptor> InternalEnumerateDescriptors() =>
        this.OnEnumerateDescriptors();
}
