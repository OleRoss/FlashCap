////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.Versioning;
using FlashCap.Utilities;

namespace FlashCap.Internal.MediaFoundation;

[SupportedOSPlatform("windows")]
internal static class MediaFoundationMediaTypes
{
    public readonly struct FormatInfo
    {
        public readonly PixelFormats PixelFormat;
        public readonly NativeMethods.Compression Compression;
        public readonly short BitCount;
        public readonly string Description;

        public FormatInfo(
            PixelFormats pixelFormat,
            NativeMethods.Compression compression,
            short bitCount,
            string description)
        {
            this.PixelFormat = pixelFormat;
            this.Compression = compression;
            this.BitCount = bitCount;
            this.Description = description;
        }
    }

    public static bool TryGetFormatInfo(Guid subtype, out FormatInfo formatInfo)
    {
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_RGB24)
        {
            formatInfo = new(PixelFormats.RGB24, NativeMethods.Compression.BI_RGB, 24, "RGB24");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_RGB32)
        {
            formatInfo = new(PixelFormats.RGB32, NativeMethods.Compression.BI_RGB, 32, "RGB32");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_ARGB32)
        {
            formatInfo = new(PixelFormats.ARGB32, NativeMethods.Compression.ARGB, 32, "ARGB32");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_RGB555)
        {
            formatInfo = new(PixelFormats.RGB15, NativeMethods.Compression.BI_RGB, 16, "RGB555");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_RGB565)
        {
            formatInfo = new(PixelFormats.RGB16, NativeMethods.Compression.D3D_RGB565, 16, "RGB565");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_MJPG)
        {
            formatInfo = new(PixelFormats.JPEG, NativeMethods.Compression.MJPG, 24, "MJPG");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_UYVY)
        {
            formatInfo = new(PixelFormats.UYVY, NativeMethods.Compression.UYVY, 16, "UYVY");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_YUY2)
        {
            formatInfo = new(PixelFormats.YUYV, NativeMethods.Compression.YUY2, 16, "YUY2");
            return true;
        }
        if (subtype == NativeMethods_MediaFoundation.MFVideoFormat_NV12)
        {
            formatInfo = new(PixelFormats.NV12, NativeMethods.Compression.NV12, 12, "NV12");
            return true;
        }

        formatInfo = default;
        return false;
    }

    public static bool TryCreateVideoCharacteristics(
        IMFMediaType mediaType,
        out VideoCharacteristics characteristics,
        out FormatInfo formatInfo)
    {
        characteristics = null!;
        formatInfo = default;

        if (mediaType.GetGUID(
            in NativeMethods_MediaFoundation.MF_MT_MAJOR_TYPE,
            out var majorType) < 0 ||
            majorType != NativeMethods_MediaFoundation.MFMediaType_Video)
        {
            return false;
        }

        if (mediaType.GetGUID(
            in NativeMethods_MediaFoundation.MF_MT_SUBTYPE,
            out var subtype) < 0 ||
            !TryGetFormatInfo(subtype, out formatInfo))
        {
            return false;
        }

        if (mediaType.GetUINT64(
            in NativeMethods_MediaFoundation.MF_MT_FRAME_SIZE,
            out var frameSize) < 0)
        {
            return false;
        }

        var frameSizeValue = unchecked((ulong)frameSize);
        var width = (int)(frameSizeValue >> 32);
        var height = (int)(frameSizeValue & 0xffffffff);
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        var fps = Fraction.Create(30);
        if (mediaType.GetUINT64(
            in NativeMethods_MediaFoundation.MF_MT_FRAME_RATE,
            out var frameRate) >= 0)
        {
            var frameRateValue = unchecked((ulong)frameRate);
            var numerator = (int)(frameRateValue >> 32);
            var denominator = (int)(frameRateValue & 0xffffffff);
            if (numerator > 0 && denominator > 0)
            {
                fps = new Fraction(numerator, denominator).Reduce();
            }
        }

        characteristics = new VideoCharacteristics(
            formatInfo.PixelFormat,
            width,
            height,
            fps,
            formatInfo.Description,
            true,
            formatInfo.Description);
        return true;
    }
}
