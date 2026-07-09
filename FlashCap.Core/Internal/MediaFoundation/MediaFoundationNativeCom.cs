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

namespace FlashCap.Internal.MediaFoundation;

internal static unsafe class MediaFoundationNativeCom
{
    // Verified against Windows SDK 10.0.26100.0 mfobjects.h.
    private const int IMFMediaBufferLockSlot = 3;
    private const int IMFMediaBufferUnlockSlot = 4;
    private const int IMFSampleConvertToContiguousBufferSlot = 41;

    public static int ConvertToContiguousBuffer(IntPtr sample, out IntPtr buffer)
    {
        buffer = IntPtr.Zero;
        fixed (IntPtr* pBuffer = &buffer)
        {
            var method = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)GetVTableEntry(
                sample,
                IMFSampleConvertToContiguousBufferSlot);
            return method(sample, pBuffer);
        }
    }

    public static int Lock(
        IntPtr mediaBuffer,
        out IntPtr data,
        out int maxLength,
        out int currentLength)
    {
        data = IntPtr.Zero;
        maxLength = 0;
        currentLength = 0;
        fixed (IntPtr* pData = &data)
        fixed (int* pMaxLength = &maxLength)
        fixed (int* pCurrentLength = &currentLength)
        {
            var method = (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int*, int*, int>)GetVTableEntry(
                mediaBuffer,
                IMFMediaBufferLockSlot);
            return method(mediaBuffer, pData, pMaxLength, pCurrentLength);
        }
    }

    public static int Unlock(IntPtr mediaBuffer)
    {
        var method = (delegate* unmanaged[Stdcall]<IntPtr, int>)GetVTableEntry(
            mediaBuffer,
            IMFMediaBufferUnlockSlot);
        return method(mediaBuffer);
    }

    private static IntPtr GetVTableEntry(IntPtr instance, int slot) =>
        Marshal.ReadIntPtr(Marshal.ReadIntPtr(instance), slot * IntPtr.Size);
}
