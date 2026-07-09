////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FlashCap;

public static class CaptureDeviceExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task StartAsync(this CaptureDevice captureDevice, CancellationToken ct = default) =>
        captureDevice.InternalStartAsync(ct);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task StopAsync(this CaptureDevice captureDevice, CancellationToken ct = default) =>
        captureDevice.InternalStopAsync(ct);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<bool> ShowPropertyPageAsync(
        this CaptureDevice captureDevice, IntPtr parentWindow, CancellationToken ct = default) =>
        captureDevice.InternalShowPropertyPageAsync(parentWindow, ct);
}
