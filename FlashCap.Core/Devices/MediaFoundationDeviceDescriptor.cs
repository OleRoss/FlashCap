#if NET8_0_OR_GREATER
////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using FlashCap.Internal;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlashCap.Devices;

public sealed class MediaFoundationDeviceDescriptor : CaptureDeviceDescriptor
{
    private readonly string symbolicLink;
    private readonly MediaFoundationInterop.Format[] formats;

    internal MediaFoundationDeviceDescriptor(
        string symbolicLink,
        string name,
        string description,
        MediaFoundationInterop.Format[] formats,
        BufferPool defaultBufferPool) :
        base(name, description, formats.Select(format => format.Characteristics).ToArray(), defaultBufferPool)
    {
        this.symbolicLink = symbolicLink;
        this.formats = formats;
    }

    public override object Identity => this.symbolicLink;

    public override DeviceTypes DeviceType => DeviceTypes.MediaFoundation;

    protected override Task<CaptureDevice> OnOpenWithFrameProcessorAsync(
        VideoCharacteristics characteristics,
        TranscodeFormats transcodeFormat,
        FrameProcessor frameProcessor,
        CancellationToken ct)
    {
        var format = this.formats.FirstOrDefault(candidate => candidate.Characteristics.Equals(characteristics));
        if (format.Characteristics is null)
        {
            throw new System.ArgumentException(
                "FlashCap: The selected Media Foundation format is not available.",
                nameof(characteristics));
        }

        return this.InternalOnOpenWithFrameProcessorAsync(
            new MediaFoundationDevice(this.symbolicLink, this.Name, format.Key),
            characteristics,
            transcodeFormat,
            frameProcessor,
            ct);
    }
}
#endif
