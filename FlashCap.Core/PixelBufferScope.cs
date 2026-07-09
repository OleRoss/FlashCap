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

public abstract class PixelBufferScope
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected PixelBufferScope(PixelBuffer buffer) =>
        this.Buffer = buffer;

    public PixelBuffer Buffer
    {
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        get;
#if NET45_OR_GREATER || NETSTANDARD || NETCOREAPP
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private set;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected virtual void OnReleaseNow() =>
        this.Buffer = null!;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void InternalReleaseNow() =>
        this.OnReleaseNow();
}
