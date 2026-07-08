////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace FlashCap.Devices;

[SupportedOSPlatform("windows")]
public sealed class MediaFoundationDeviceDescriptor : CaptureDeviceDescriptor
{
    private readonly string symbolicLink;

    internal MediaFoundationDeviceDescriptor(
        string symbolicLink,
        string name,
        string description,
        VideoCharacteristics[] characteristics,
        BufferPool defaultBufferPool) :
        base(name, description, characteristics, defaultBufferPool) =>
        this.symbolicLink = symbolicLink;

    public override object Identity =>
        this.symbolicLink;

    public override DeviceTypes DeviceType =>
        DeviceTypes.MediaFoundation;

    protected override Task<CaptureDevice> OnOpenWithFrameProcessorAsync(
        VideoCharacteristics characteristics,
        TranscodeFormats transcodeFormat,
        FrameProcessor frameProcessor,
        CancellationToken ct) =>
        this.InternalOnOpenWithFrameProcessorAsync(
            new MediaFoundationDevice(this.symbolicLink, this.Name),
            characteristics,
            transcodeFormat,
            frameProcessor,
            ct);
}
