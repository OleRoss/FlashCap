////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.Runtime.CompilerServices;

namespace FlashCap;

public static class PixelBufferScopeExtension
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ReleaseNow(
        this PixelBufferScope pixelBufferScope) =>
        pixelBufferScope.InternalReleaseNow();
}
