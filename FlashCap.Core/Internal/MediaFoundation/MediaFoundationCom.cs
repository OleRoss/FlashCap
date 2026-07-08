////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace FlashCap.Internal.MediaFoundation;

internal static class MediaFoundationCom
{
    private sealed class MediaFoundationComWrappers : StrategyBasedComWrappers
    {
        public void Release(object value) =>
            base.ReleaseObjects(new[] { value });
    }

    private static readonly MediaFoundationComWrappers Wrappers = new();

    public static T Wrap<T>(IntPtr pointer)
        where T : class
    {
        if (pointer == IntPtr.Zero)
        {
            throw new ArgumentNullException(nameof(pointer));
        }

        return (T)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.None);
    }

    public static void ReleaseObject(object? value)
    {
        if (value != null)
        {
            Wrappers.Release(value);
        }
    }

    public static void Release(IntPtr pointer)
    {
        if (pointer != IntPtr.Zero)
        {
            Marshal.Release(pointer);
        }
    }

    public static string GetAllocatedString(IMFAttributes attributes, in Guid key)
    {
        var hr = attributes.GetAllocatedString(in key, out var value, out _);
        if (hr < 0 || value == IntPtr.Zero)
        {
            return string.Empty;
        }

        try
        {
            return Marshal.PtrToStringUni(value) ?? string.Empty;
        }
        finally
        {
            Marshal.FreeCoTaskMem(value);
        }
    }
}
