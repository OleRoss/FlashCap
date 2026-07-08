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
using System.Runtime.CompilerServices;

namespace FlashCap;

public class CaptureDevices
{
    protected readonly BufferPool DefaultBufferPool;

    public CaptureDevices() :
        this(new DefaultBufferPool())
    {
    }
    
    public CaptureDevices(BufferPool defaultBufferPool) =>
        this.DefaultBufferPool = defaultBufferPool;

    protected virtual IEnumerable<CaptureDeviceDescriptor> OnEnumerateDescriptors()
    {
        if (OperatingSystem.IsWindows())
            return new MediaFoundationDevices(this.DefaultBufferPool).OnEnumerateDescriptors();
        if (OperatingSystem.IsLinux())
            return new V4L2Devices(this.DefaultBufferPool).OnEnumerateDescriptors();
        if (OperatingSystem.IsMacOS())
            return new AVFoundationDevices(this.DefaultBufferPool).OnEnumerateDescriptors();
        return ArrayEx.Empty<CaptureDeviceDescriptor>();
    }

    internal IEnumerable<CaptureDeviceDescriptor> InternalEnumerateDescriptors() =>
        this.OnEnumerateDescriptors();
}
