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
using System.Runtime.Versioning;

namespace FlashCap.Internal;

[SupportedOSPlatform("windows")]
internal static partial class NativeMethods_MediaFoundation
{
    public const int S_OK = 0;
    public const int S_FALSE = 1;

    public const int MF_VERSION = 0x00020070;
    public const int MFSTARTUP_FULL = 0;

    public const int MF_SOURCE_READER_FIRST_VIDEO_STREAM = unchecked((int)0xfffffffc);
    public const int MF_SOURCE_READER_ALL_STREAMS = unchecked((int)0xfffffffe);
    public const int MF_SOURCE_READER_ANY_STREAM = unchecked((int)0xfffffffe);

    public const int MF_SOURCE_READERF_ENDOFSTREAM = 0x00000002;
    public const int MF_SOURCE_READERF_NATIVEMEDIATYPECHANGED = 0x00000010;
    public const int MF_SOURCE_READERF_CURRENTMEDIATYPECHANGED = 0x00000020;
    public const int MF_SOURCE_READERF_STREAMTICK = 0x00000100;

    public const int MF_E_NO_MORE_TYPES = unchecked((int)0xc00d36b9);

    public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE =
        new("c60ac5fe-252a-478f-a0ef-bc8fa5f7cad3");
    public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID =
        new("8ac3587a-4ae7-42d8-99e0-0a6013eef90f");
    public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME =
        new("60d0e559-52f8-4fa2-bbce-acdb34a8ec01");
    public static readonly Guid MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK =
        new("58f0aad8-22bf-4f8a-bb3d-d2c4978c6e2f");

    public static readonly Guid MF_MT_MAJOR_TYPE =
        new("48eba18e-f8c9-4687-bf11-0a74c9f96a8f");
    public static readonly Guid MF_MT_SUBTYPE =
        new("f7e34c9a-42e8-4714-b74b-cb29d72c35e5");
    public static readonly Guid MF_MT_FRAME_SIZE =
        new("1652c33d-d6b2-4012-b834-72030849a37d");
    public static readonly Guid MF_MT_FRAME_RATE =
        new("c459a2e8-3d2c-4e44-b132-fee5156c7bb0");
    public static readonly Guid MF_MT_DEFAULT_STRIDE =
        new("644b4e48-1e02-4516-b0eb-c01ca9d49ac6");

    public static readonly Guid MFMediaType_Video =
        new("73646976-0000-0010-8000-00aa00389b71");

    public static readonly Guid MFVideoFormat_RGB24 =
        new("00000014-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_RGB32 =
        new("00000016-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_ARGB32 =
        new("00000015-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_RGB555 =
        new("00000018-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_RGB565 =
        new("00000017-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_MJPG =
        new("47504a4d-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_UYVY =
        new("59565955-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_YUY2 =
        new("32595559-0000-0010-8000-00aa00389b71");
    public static readonly Guid MFVideoFormat_NV12 =
        new("3231564e-0000-0010-8000-00aa00389b71");

    public static readonly Guid IID_IMFMediaSource =
        new("279a808d-aec7-40c8-9c6b-a6b492c78a66");

    [LibraryImport("mfplat.dll")]
    public static partial int MFStartup(
        int version,
        int flags);

    [LibraryImport("mfplat.dll")]
    public static partial int MFShutdown();

    [LibraryImport("mfplat.dll")]
    public static partial int MFCreateAttributes(
        out IntPtr attributes,
        int initialSize);

    [LibraryImport("mfplat.dll")]
    public static partial int MFCreateMediaType(
        out IntPtr mediaType);

    [LibraryImport("mf.dll")]
    public static partial int MFEnumDeviceSources(
        IntPtr attributes,
        out IntPtr activateArray,
        out int count);

    [LibraryImport("mfreadwrite.dll")]
    public static partial int MFCreateSourceReaderFromMediaSource(
        IntPtr mediaSource,
        IntPtr attributes,
        out IntPtr sourceReader);

    public static void ThrowIfFailed(int hr, string operation)
    {
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
            throw new InvalidOperationException($"FlashCap: {operation} failed: HR=0x{hr:x8}");
        }
    }
}
