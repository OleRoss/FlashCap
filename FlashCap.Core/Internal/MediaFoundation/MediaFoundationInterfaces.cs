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

[GeneratedComInterface]
[Guid("2cd2d921-c447-44a7-a13c-4adabfc247e3")]
internal partial interface IMFAttributes
{
    [PreserveSig]
    int GetItem(in Guid guidKey, IntPtr value);

    [PreserveSig]
    int GetItemType(in Guid guidKey, out int type);

    [PreserveSig]
    int CompareItem(in Guid guidKey, IntPtr value, out int result);

    [PreserveSig]
    int Compare(IntPtr attributes, int matchType, out int result);

    [PreserveSig]
    int GetUINT32(in Guid guidKey, out int value);

    [PreserveSig]
    int GetUINT64(in Guid guidKey, out long value);

    [PreserveSig]
    int GetDouble(in Guid guidKey, out double value);

    [PreserveSig]
    int GetGUID(in Guid guidKey, out Guid value);

    [PreserveSig]
    int GetStringLength(in Guid guidKey, out int length);

    [PreserveSig]
    int GetString(in Guid guidKey, IntPtr value, int size, out int length);

    [PreserveSig]
    int GetAllocatedString(in Guid guidKey, out IntPtr value, out int length);

    [PreserveSig]
    int GetBlobSize(in Guid guidKey, out int size);

    [PreserveSig]
    int GetBlob(in Guid guidKey, IntPtr buffer, int bufferSize, out int blobSize);

    [PreserveSig]
    int GetAllocatedBlob(in Guid guidKey, out IntPtr buffer, out int size);

    [PreserveSig]
    int GetUnknown(in Guid guidKey, in Guid riid, out IntPtr value);

    [PreserveSig]
    int SetItem(in Guid guidKey, IntPtr value);

    [PreserveSig]
    int DeleteItem(in Guid guidKey);

    [PreserveSig]
    int DeleteAllItems();

    [PreserveSig]
    int SetUINT32(in Guid guidKey, int value);

    [PreserveSig]
    int SetUINT64(in Guid guidKey, long value);

    [PreserveSig]
    int SetDouble(in Guid guidKey, double value);

    [PreserveSig]
    int SetGUID(in Guid guidKey, in Guid value);

    [PreserveSig]
    int SetString(in Guid guidKey, [MarshalAs(UnmanagedType.LPWStr)] string value);

    [PreserveSig]
    int SetBlob(in Guid guidKey, IntPtr buffer, int size);

    [PreserveSig]
    int SetUnknown(in Guid guidKey, IntPtr unknown);

    [PreserveSig]
    int LockStore();

    [PreserveSig]
    int UnlockStore();

    [PreserveSig]
    int GetCount(out int count);

    [PreserveSig]
    int GetItemByIndex(int index, out Guid guidKey, IntPtr value);

    [PreserveSig]
    int CopyAllItems(IntPtr destination);
}

[GeneratedComInterface]
[Guid("44ae0fa8-ea31-4109-8d2e-4cae4997c555")]
internal partial interface IMFMediaType : IMFAttributes
{
    [PreserveSig]
    int GetMajorType(out Guid majorType);

    [PreserveSig]
    int IsCompressedFormat(out int compressed);

    [PreserveSig]
    int IsEqual(IntPtr mediaType, out int flags);

    [PreserveSig]
    int GetRepresentation(in Guid representation, out IntPtr value);

    [PreserveSig]
    int FreeRepresentation(in Guid representation, IntPtr value);
}

[GeneratedComInterface]
[Guid("7fee9e9a-4a89-47a6-899c-b6a53a70fb67")]
internal partial interface IMFActivate : IMFAttributes
{
    [PreserveSig]
    int ActivateObject(in Guid riid, out IntPtr value);

    [PreserveSig]
    int ShutdownObject();

    [PreserveSig]
    int DetachObject();
}

[GeneratedComInterface]
[Guid("2cd0bd52-bcd5-4b89-b62c-eadc0c031e7d")]
internal partial interface IMFMediaEventGenerator
{
    [PreserveSig]
    int GetEvent(int flags, out IntPtr mediaEvent);

    [PreserveSig]
    int BeginGetEvent(IntPtr callback, IntPtr state);

    [PreserveSig]
    int EndGetEvent(IntPtr result, out IntPtr mediaEvent);

    [PreserveSig]
    int QueueEvent(int met, in Guid extendedType, int status, IntPtr value);
}

[GeneratedComInterface]
[Guid("279a808d-aec7-40c8-9c6b-a6b492c78a66")]
internal partial interface IMFMediaSource : IMFMediaEventGenerator
{
    [PreserveSig]
    int GetCharacteristics(out int characteristics);

    [PreserveSig]
    int CreatePresentationDescriptor(out IntPtr presentationDescriptor);

    [PreserveSig]
    int Start(IntPtr presentationDescriptor, in Guid timeFormat, IntPtr startPosition);

    [PreserveSig]
    int Stop();

    [PreserveSig]
    int Pause();

    [PreserveSig]
    int Shutdown();
}

[GeneratedComInterface]
[Guid("70ae66f2-c809-4e4f-8915-bdcb406b7993")]
internal partial interface IMFSourceReader
{
    [PreserveSig]
    int GetStreamSelection(int streamIndex, out int selected);

    [PreserveSig]
    int SetStreamSelection(int streamIndex, int selected);

    [PreserveSig]
    int GetNativeMediaType(int streamIndex, int mediaTypeIndex, out IntPtr mediaType);

    [PreserveSig]
    int GetCurrentMediaType(int streamIndex, out IntPtr mediaType);

    [PreserveSig]
    int SetCurrentMediaType(int streamIndex, IntPtr reserved, IntPtr mediaType);

    [PreserveSig]
    int SetCurrentPosition(in Guid timeFormat, IntPtr position);

    [PreserveSig]
    int ReadSample(
        int streamIndex,
        int controlFlags,
        out int actualStreamIndex,
        out int streamFlags,
        out long timestamp,
        out IntPtr sample);

    [PreserveSig]
    int Flush(int streamIndex);

    [PreserveSig]
    int GetServiceForStream(int streamIndex, in Guid service, in Guid riid, out IntPtr value);

    [PreserveSig]
    int GetPresentationAttribute(int streamIndex, in Guid guidAttribute, IntPtr value);
}

[GeneratedComInterface]
[Guid("045fa593-8799-42b8-bc8d-8968c6453507")]
internal partial interface IMFMediaBuffer
{
    [PreserveSig]
    int Lock(out IntPtr buffer, out int maxLength, out int currentLength);

    [PreserveSig]
    int Unlock();

    [PreserveSig]
    int GetCurrentLength(out int currentLength);

    [PreserveSig]
    int SetCurrentLength(int currentLength);

    [PreserveSig]
    int GetMaxLength(out int maxLength);
}

[GeneratedComInterface]
[Guid("c40a00f2-b93a-4d80-ae8c-5a1c634f58e4")]
internal partial interface IMFSample : IMFAttributes
{
    [PreserveSig]
    int GetSampleFlags(out int sampleFlags);

    [PreserveSig]
    int SetSampleFlags(int sampleFlags);

    [PreserveSig]
    int GetSampleTime(out long sampleTime);

    [PreserveSig]
    int SetSampleTime(long sampleTime);

    [PreserveSig]
    int GetSampleDuration(out long sampleDuration);

    [PreserveSig]
    int SetSampleDuration(long sampleDuration);

    [PreserveSig]
    int GetBufferCount(out int bufferCount);

    [PreserveSig]
    int GetBufferByIndex(int index, out IntPtr buffer);

    [PreserveSig]
    int ConvertToContiguousBuffer(out IntPtr buffer);

    [PreserveSig]
    int AddBuffer(IntPtr buffer);

    [PreserveSig]
    int RemoveBufferByIndex(int index);

    [PreserveSig]
    int RemoveAllBuffers();

    [PreserveSig]
    int GetTotalLength(out int totalLength);

    [PreserveSig]
    int CopyToBuffer(IntPtr buffer);
}
