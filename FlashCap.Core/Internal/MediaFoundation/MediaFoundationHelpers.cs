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
using System.Diagnostics;
using Windows.Win32.Foundation;

namespace FlashCap.Internal.MediaFoundation;

internal static class MediaFoundationHelpers
{
    internal static void ThrowIfFailed(HRESULT result, string operation)
    {
        if (result.Failed)
        {
            throw new InvalidOperationException(
                $"FlashCap: {operation} failed (HRESULT=0x{unchecked((uint)result.Value):X8}).");
        }
    }

    internal static void TraceFailure(string operation, Exception exception) =>
        Trace.WriteLine($"FlashCap: Media Foundation {operation} failed: {exception}");
}
#endif