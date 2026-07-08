////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Yoh Deadfall (@YohDeadfall)
// Copyright (c) Felipe Ferreira Quintella (@ffquintella)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using FlashCap.Internal.AVFoundation;

namespace FlashCap.Internal;

[SupportedOSPlatform("macos")]
internal static partial class NativeMethods_AVFoundation
{
    public static readonly Dictionary<PixelFormats, int> PixelFormatMap = new()
    {
        //[PixelFormats.UYVY] = LibCoreVideo.PixelFormatType_24RGB,
        [PixelFormats.RGB32] = 32,
        //[PixelFormats.ARGB32] = LibCoreVideo.PixelFormatType_32BGRA,
        //[PixelFormats.BGRA32] = LibCoreVideo.PixelFormatType_32BGRA,
        [PixelFormats.ARGB32] = LibCoreVideo.PixelFormatType_32BGRA,
        [PixelFormats.RGB24] = LibCoreVideo.PixelFormatType_24RGB,
        [PixelFormats.UYVY] = LibCoreVideo.PixelFormatType_422YpCbCr8_yuvs,
        [PixelFormats.YUYV] = LibCoreVideo.PixelFormatType_422YpCbCr8,
        
    };

    public static partial class Dlfcn
    {
        // Loads the framework
        [LibraryImport("libdl.dylib", EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr dlopen(string path, int mode);
        
        [LibraryImport("libdl.dylib", EntryPoint = "dlsym", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr dlsym(IntPtr handle, string symbol);
        
        [LibraryImport(LibSystem.Path, EntryPoint = "dlopen", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr OpenLibrary(string path, Mode mode);

        [LibraryImport(LibSystem.Path, EntryPoint = "dlsym", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr GetSymbol(IntPtr handle, string symbol);

        public static IntPtr GetSymbolIndirect(IntPtr handle, string symbol) =>
            GetSymbol(handle, symbol) is var indirect && indirect != IntPtr.Zero
                ? Marshal.ReadIntPtr(indirect)
                : IntPtr.Zero;

        [Flags]
        public enum Mode : int
        {
            None = 0x0,
            Now = 2,
        }
    }

    public static partial class LibSystem
    {
        public const string Path = "/usr/lib/libSystem.dylib";

        public static readonly IntPtr Handle = Dlfcn.OpenLibrary(Path, Dlfcn.Mode.None);
        
        [LibraryImport(Path, EntryPoint = "dispatch_queue_create", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr dispatch_queue_create(string label, IntPtr attr);

        public static readonly bool IsOnArm64;

        static unsafe LibSystem()
        {
            IsOnArm64 =
                IntPtr.Size == 8 &&
                NXGetLocalArchInfo()->GetName()?.StartsWith("arm64", StringComparison.OrdinalIgnoreCase) is true;
        }

        [LibraryImport(Path, EntryPoint = "NXGetLocalArchInfo")]
        private static unsafe partial NXArchInfo* NXGetLocalArchInfo();

        private enum NXByteOrder
        {
            Unknown,
            LittleEndian,
            BigEndian,
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NXArchInfo
        {
            public IntPtr Name;
            public int CpuType;
            public int CpuSubType;
            public NXByteOrder ByteOrder;
            public IntPtr Description;

            public string? GetName() => GetString(Name);

            public string? GetDescription() => GetString(Description);

            private static unsafe string? GetString(IntPtr pointer)
            {
                if (pointer == IntPtr.Zero)
                    return null;

                var start = (byte*)pointer;
                var end = (byte*)pointer;

                for (; *end != 0; end += 1) ;

                return Encoding.UTF8.GetString(start, new IntPtr(end - start).ToInt32());
            }
        }
    }

    private static partial class LibC
    {
        private const string Path = "/usr/lib/libc.dylib";

        [LibraryImport(Path, EntryPoint = "dispatch_queue_create", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr DispatchQueueCreate(string label, IntPtr attr);

        [LibraryImport(Path, EntryPoint = "dispatch_release")]
        public static partial IntPtr DispatchRelease(IntPtr o);

        [LibraryImport(Path, EntryPoint = "dispatch_retain")]
        public static partial IntPtr DispatchRetain(IntPtr o);
    }

    public static partial class LibObjC
    {
        private const string Path = "/usr/lib/libobjc.A.dylib";

        public const string InitSelector = "init";
        public const string AllocSelector = "alloc";
        public const string ReleaseSelector = "release";

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial void SendNoResult(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.I1)] bool arg1);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial void SendNoResult(IntPtr receiver, IntPtr selector);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial void SendNoResult(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial void SendNoResult(IntPtr receiver, IntPtr selector, LibCoreMedia.CMTime arg1);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial void SendNoResult(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool SendAndGetBool(IntPtr receiver, IntPtr selector);
        
        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool SendAndGetBool(IntPtr receiver, IntPtr selector, IntPtr arg1);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendAndGetHandle(IntPtr receiver, IntPtr selector);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendAndGetHandle(IntPtr receiver, IntPtr selector, IntPtr arg1);
        
        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendAndGetHandle(IntPtr receiver, IntPtr selector, int arg1);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendAndGetHandle(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial IntPtr SendAndGetHandle(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, long arg3);

        [LibraryImport(Path, EntryPoint = "objc_msgSend")]
        public static partial LibCoreMedia.CMTime SendAndGetCMTime(IntPtr receiver, IntPtr selector);

        [LibraryImport(Path, EntryPoint = "objc_msgSend_stret")]
        public static partial void SendAndGetCMTimeStret(out LibCoreMedia.CMTime result, IntPtr receiver, IntPtr selector);

        [LibraryImport(Path, EntryPoint = "objc_getClass", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr GetClass(string name);

        [LibraryImport(Path, EntryPoint = "object_getIvar")]
        public static partial IntPtr GetVariable(IntPtr obj, IntPtr ivar);

        [LibraryImport(Path, EntryPoint = "object_setIvar")]
        public static partial void SetVariable(IntPtr obj, IntPtr ivar, IntPtr value);

        [LibraryImport(Path, EntryPoint = "objc_allocateClassPair", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr AllocateClass(IntPtr superclass, string name, IntPtr extraBytes);

        [LibraryImport(Path, EntryPoint = "objc_registerClassPair")]
        public static partial void RegisterClass(IntPtr cls);

        [LibraryImport(Path, EntryPoint = "objc_getProtocol", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr GetProtocol(string name);

        [LibraryImport(Path, EntryPoint = "sel_registerName", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr GetSelector(string name);

        [LibraryImport(Path, EntryPoint = "class_addMethod", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool AddMethod(IntPtr cls, IntPtr name, IntPtr imp, string types);

        [LibraryImport(Path, EntryPoint = "class_addIvar", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial void AddVariable(IntPtr cls, string name, IntPtr size, byte alignment, string types);

        [LibraryImport(Path, EntryPoint = "class_addProtocol")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool AddProtocol(IntPtr cls, IntPtr protocol);

        [LibraryImport(Path, EntryPoint = "class_getInstanceVariable", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr GetVariable(IntPtr cls, string name);
        
        [LibraryImport(Path, EntryPoint = "objc_allocateClassPair", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr objc_allocateClassPair(IntPtr superClass, string name, IntPtr extraBytes);

        [LibraryImport(Path, EntryPoint = "class_addMethod", StringMarshalling = StringMarshalling.Utf8)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string types);
        
        [LibraryImport(Path, EntryPoint = "objc_registerClassPair")]
        public static partial void objc_registerClassPair(IntPtr cls);
        
        [LibraryImport(Path, EntryPoint = "dispatch_queue_create", StringMarshalling = StringMarshalling.Utf8)]
        public static partial IntPtr dispatch_queue_create(string label, IntPtr attr);
        
        [Flags]
        public enum BlockFlags
        {
            BLOCK_REFCOUNT_MASK = 0xffff,
            BLOCK_NEEDS_FREE = 1 << 24,
            BLOCK_HAS_COPY_DISPOSE = 1 << 25,
            BLOCK_HAS_CTOR = 1 << 26,
            BLOCK_IS_GC = 1 << 27,
            BLOCK_IS_GLOBAL = 1 << 28,
            BLOCK_HAS_STRET = 1 << 29,
            BLOCK_HAS_SIGNATURE = 1 << 30,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct BlockDescriptor
        {
            public IntPtr Reserved;
            public IntPtr Size;
            public IntPtr Copy;
            public IntPtr Dispose;
            public IntPtr Signature;

            public static IntPtr Create(string signature, Delegate target, BlockLiteralCopy copy, BlockLiteralDispose dispose)
            {
                var signatureBytes = Encoding.UTF8.GetBytes(signature);
                var descriptorSize = MarshalEx.SizeOf<BlockDescriptor>();

                nint memory = Marshal.AllocHGlobal(descriptorSize + signatureBytes.Length);
                var memoryToSignature = memory + descriptorSize;

                Marshal.Copy(signatureBytes, startIndex: 0, memoryToSignature, length: 0);

                unsafe
                {
                    var descriptor = (BlockDescriptor*)memory;

                    descriptor->Size = new IntPtr(MarshalEx.SizeOf<BlockLiteral>());
                    descriptor->Copy = Marshal.GetFunctionPointerForDelegate(copy);
                    descriptor->Dispose = Marshal.GetFunctionPointerForDelegate(dispose);
                    descriptor->Signature = memoryToSignature;
                }

                return memory;
            }
        }

        public unsafe delegate void BlockLiteralCopy(BlockLiteral* dst, BlockLiteral* src);
        public unsafe delegate void BlockLiteralDispose(BlockLiteral* self);

        // https://clang.llvm.org/docs/Block-ABI-Apple.html
        public unsafe struct BlockLiteral : IDisposable
        {
            public IntPtr Isa;
            public BlockFlags Flags;
            public int Reserved;
            public IntPtr Invoke;
            public IntPtr Descriptor;
            public IntPtr ContextHandle;

            public void Dispose()
            {
                if (ContextHandle != IntPtr.Zero)
                {
                    GCHandle
                        .FromIntPtr(ContextHandle)
                        .Free();

                    ContextHandle = IntPtr.Zero;
                }
            }

            public static unsafe void Dispose(BlockLiteral* self) =>
                self->Dispose();

            public static unsafe void Copy(BlockLiteral* dst, BlockLiteral* src)
            {
                var context = GCHandle.FromIntPtr(src->ContextHandle);

                dst->ContextHandle = (IntPtr)GCHandle.Alloc(context.Target);
                dst->Descriptor = src->Descriptor;
            }

            public static TDelegate GetTarget<TDelegate>(IntPtr block)
                where TDelegate : class
            {
                var self = (BlockLiteral*)block;
                var target = GCHandle.FromIntPtr(self->ContextHandle).Target;

                return (TDelegate)target!;
            }
        }

        public sealed class BlockLiteralFactory
        {
            private static readonly IntPtr NSConcreteStackBlock;
            private static readonly BlockLiteralCopy CopyHandler;
            private static readonly BlockLiteralDispose DisposeHandler;

            static unsafe BlockLiteralFactory()
            {
                NSConcreteStackBlock = Dlfcn.GetSymbol(LibSystem.Handle, "_NSConcreteStackBlock");

                CopyHandler = BlockLiteral.Copy;
                DisposeHandler = BlockLiteral.Dispose;
            }

            private readonly IntPtr _trampoline;
            private readonly IntPtr _descriptor;

            private BlockLiteralFactory(IntPtr trampoline, IntPtr descriptor)
            {
                _trampoline = trampoline;
                _descriptor = descriptor;
            }

            public static BlockLiteralFactory CreateFactory<T>(string signature, T trampoline)
                where T : Delegate
            {
                var descriptor = DescriptorCache<T>.Instance;
                if (descriptor == IntPtr.Zero)
                {
                    descriptor = BlockDescriptor.Create(signature, trampoline, CopyHandler, DisposeHandler);

                    var descriptorCached = Interlocked.CompareExchange(ref DescriptorCache<T>.Instance, descriptor, default);
                    if (descriptorCached != default)
                    {
                        Marshal.FreeHGlobal(descriptor);

                        descriptor = descriptorCached;
                    }
                }

                return new BlockLiteralFactory(Marshal.GetFunctionPointerForDelegate(trampoline), descriptor);
            }

            public BlockLiteral CreateLiteral(object? context)
            {
                return new BlockLiteral
                {
                    Isa = NSConcreteStackBlock,
                    Flags = BlockFlags.BLOCK_HAS_COPY_DISPOSE | BlockFlags.BLOCK_HAS_SIGNATURE,
                    Invoke = _trampoline,
                    Descriptor = _descriptor,
                    ContextHandle = (IntPtr)GCHandle.Alloc(context),
                };
            }

            private static class DescriptorCache<T>
                where T : Delegate
            {
                public static IntPtr Instance;
            }
        }
        
        public static IntPtr CreateNSNumber(int value)
        {
            IntPtr nsNumberClass = LibObjC.GetClass("NSNumber");
            IntPtr sel = LibObjC.GetSelector("numberWithInt:");
            return LibObjC.SendAndGetHandle(nsNumberClass, sel, value);
        }

        public abstract class NSObject : NativeObject
        {
            protected NSObject(IntPtr handle, bool retain)
            {
                Handle = handle;

                if (retain)
                    LibCoreFoundation.CFRetain(handle);
            }

            protected override void Dispose(bool disposing)
            {
                if (Handle == IntPtr.Zero)
                    return;

                LibObjC.SendNoResult(
                    Handle,
                    LibObjC.GetSelector(LibObjC.ReleaseSelector));

                Handle = IntPtr.Zero;
            }
        }

        public sealed class NSError : NSObject
        {
            public NSError(IntPtr handle, bool retain) :
                base(handle, retain)
            { }

            public string Error { get; set; } = string.Empty;
        }
    }

    public static partial class LibCoreFoundation
    {
        private const string Path = "/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation";

        public static readonly IntPtr Handle = Dlfcn.OpenLibrary(Path, Dlfcn.Mode.Now);
        public static readonly IntPtr kCFTypeArrayCallbacks = Dlfcn.GetSymbol(Handle, "kCFTypeArrayCallBacks");
        public static readonly IntPtr kCFCopyStringDictionaryKeyCallBacks = Dlfcn.GetSymbolIndirect(Handle, "kCFCopyStringDictionaryKeyCallBacks");
        public static readonly IntPtr kCFTypeDictionaryValueCallBacks = Dlfcn.GetSymbolIndirect(Handle, "kCFTypeDictionaryValueCallBacks");

        [LibraryImport(Path, EntryPoint = "CFRelease")]
        public static partial void CFRelease(IntPtr cf);
        
        [LibraryImport(Path, EntryPoint = "CFGetTypeID")]
        public static partial IntPtr CFGetTypeID(IntPtr cf);

        [LibraryImport(Path, EntryPoint = "CFRetain")]
        public static partial void CFRetain(IntPtr cf);

        [LibraryImport(Path, EntryPoint = "CFArrayCreate")]
        public static partial IntPtr CFArrayCreate(IntPtr allocator, IntPtr values, nint numValues, IntPtr callBacks);

        [LibraryImport(Path, EntryPoint = "CFArrayGetCount")]
        public static partial IntPtr CFArrayGetCount(IntPtr theArray);

        [LibraryImport(Path, EntryPoint = "CFArrayGetValues")]
        public static partial void CFArrayGetValues(IntPtr theArray, CFRange range, IntPtr values);

        [LibraryImport(Path, EntryPoint = "CFStringGetLength")]
        public static partial IntPtr CFStringGetLength(IntPtr theString);

        [LibraryImport(Path, EntryPoint = "CFStringGetCharactersPtr")]
        public static unsafe partial char* CFStringGetCharactersPtr(IntPtr theString);

        [LibraryImport(Path, EntryPoint = "CFStringGetCharacters")]
        public static unsafe partial void CFStringGetCharacters(IntPtr theString, CFRange range, char* buffer);

        [LibraryImport(Path, EntryPoint = "CFStringCreateWithCharacters")]
        public static unsafe partial IntPtr CFStringCreateWithCharacters(IntPtr allocator, char* str, nint count);

        [LibraryImport(Path, EntryPoint = "CFNumberCreate")]
        public static unsafe partial IntPtr CFNumberCreate(IntPtr allocator, CFNumberType theType, void* valuePtr);

        [LibraryImport(Path, EntryPoint = "CFNumberGetValue")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static unsafe partial bool CFNumberGetValue(IntPtr number, CFNumberType theType, void* valuePtr);

        [LibraryImport(Path, EntryPoint = "CFDictionaryCreate")]
        private static unsafe partial IntPtr CFDictionaryCreate(IntPtr allocator, IntPtr* keys, IntPtr* values, nint numValues, IntPtr keyCallBacks, IntPtr valueCallBacks);

        public static unsafe IntPtr CFDictionaryCreate(IntPtr allocator, IntPtr[] keys, IntPtr[] values, nint numValues, IntPtr keyCallBacks, IntPtr valueCallBacks)
        {
            fixed (IntPtr* keysPointer = keys)
            fixed (IntPtr* valuesPointer = values)
            {
                return CFDictionaryCreate(allocator, keysPointer, valuesPointer, numValues, keyCallBacks, valueCallBacks);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CFRange
        {
            public nint Location;
            public nint Length;

            public CFRange(nint location, nint length)
            {
                Location = new IntPtr(location);
                Length = new IntPtr(length);
            }
        }

        public abstract class CFObject : NativeObject
        {
            protected CFObject(IntPtr handle) => Handle = handle;

            protected override void Dispose(bool disposing)
            {
                if (Handle == IntPtr.Zero)
                    return;

                CFRelease(Handle);

                Handle = IntPtr.Zero;
            }
        }
        
        public static class BasicClassesHelper
        {
            public static IntPtr NSString => LibObjC.GetClass("NSString");
            public static IntPtr NSNumber => LibObjC.GetClass("NSNumber");
            public static IntPtr NSArray => LibObjC.GetClass("NSArray");
            public static IntPtr NSDictionary => LibObjC.GetClass("NSDictionary");
        }

        // https://github.com/opensource-apple/CF/blob/master/CFArray.c
        public sealed class CFArray : CFObject
        {
            public CFArray(IntPtr handle) :
                base(handle)
            { }

            public static unsafe CFArray Create(IntPtr[] items)
            {
                fixed (IntPtr* pointer = items)
                {
                    return new CFArray(CFArrayCreate(IntPtr.Zero, new IntPtr(pointer), items.Length, kCFTypeArrayCallbacks));
                }
            }

            public static unsafe T[] ToArray<T>(IntPtr handle, Func<IntPtr, T> constructor)
            {
                var count = CFArrayGetCount(handle).ToInt32();
                if (count == 0)
                    return ArrayEx.Empty<T>();

                var buffer = new IntPtr[count];

                unsafe
                {
                    fixed (void* ptr = buffer)
                        CFArrayGetValues(handle, new CFRange(location: 0, count), new IntPtr(ptr));
                }

                var array = new T[count];
                for (var i = 0; i < count; i++)
                {
                    array[i] = constructor(buffer[i]);
                }

                return array;
            }
        }

        // https://github.com/opensource-apple/CF/blob/master/CFString.c
        public sealed class CFString : CFObject
        {
            public CFString(IntPtr handle) :
                base(handle)
            { }

            public override unsafe string? ToString()
            {
                if (Handle == IntPtr.Zero)
                    return null;

                int length = LibCoreFoundation.CFStringGetLength(Handle).ToInt32();
                if (length == 0)
                    return string.Empty;

                var chars = LibCoreFoundation.CFStringGetCharactersPtr(Handle);
                var memoryAllocated = false;

                if (chars is null)
                {
                    var heapAllocated = length > 128;
                    if (heapAllocated)
                        chars = (char*)Marshal.AllocHGlobal(length * 2);
                    else
                    {
                        var buffer = stackalloc char[length];
                        chars = buffer;
                    }

                    LibCoreFoundation.CFStringGetCharacters(Handle, new CFRange(0, length), chars);
                }

                var result = new string(chars, 0, length);

                if (memoryAllocated)
                    Marshal.FreeHGlobal(new IntPtr(chars));

                return result;
            }

            public static unsafe CFString Create(string value)
            {
                fixed (char* chars = value)
                {
                    return new(LibCoreFoundation.CFStringCreateWithCharacters(
                        allocator: IntPtr.Zero, chars, value.Length));
                }
            }
        }

        public enum CFNumberType
        {
            sInt32Type = 3
        }

        public sealed class DispatchQueue : NativeObject
        {
            public DispatchQueue(string label)
            {
                //Handle = LibC.DispatchQueueCreate(label, IntPtr.Zero) is var handle && handle != IntPtr.Zero
                //    ? handle : throw new InvalidOperationException("Cannot create a dispatch queue.");

                Handle = LibSystem.dispatch_queue_create(label, IntPtr.Zero);
                CFRetain(Handle);
            }


            protected override void Dispose(bool disposing) =>
                LibC.DispatchRelease(Handle);
        }
    }
    
    public enum CMBlockBufferError : int {
        None						= 0,
        StructureAllocationFailed	= -12700,
        BlockAllocationFailed		= -12701,
        BadCustomBlockSource		= -12702,
        BadOffsetParameter			= -12703,
        BadLengthParameter			= -12704,
        BadPointerParameter			= -12705,
        EmptyBlockBuffer			= -12706,
        UnallocatedBlock			= -12707,
        InsufficientSpace			= -12708,
    }

    public static partial class LibCoreMedia
    {
        private const string Path = "/System/Library/Frameworks/CoreMedia.framework/CoreMedia";

        [LibraryImport(Path, EntryPoint = "CMSampleBufferGetAttachments")]
        public static partial IntPtr CMSampleBufferGetAttachments(IntPtr sampleBuffer, int makeWritable);

        [LibraryImport(Path, EntryPoint = "CMGetAttachment")]
        public static partial IntPtr CMGetAttachment(IntPtr target, IntPtr key, out CMAttachmentMode attachmentMode);

        [LibraryImport(Path, EntryPoint = "CMFormatDescriptionGetMediaType")]
        public static partial uint CmFormatDescriptionGetMediaTypeIntCode(IntPtr formatDescription);
        
        // Add this enum
        public enum CMAttachmentMode
        {
            ShouldNotPropagate = 0,
            ShouldPropagate = 1
        }
        
        // Add this constant
        public static readonly FourCharCode kCMMediaType_Video = new FourCharCode('v', 'i', 'd', 'e');
        
        [LibraryImport(Path, EntryPoint = "CMTimeMake")]
        public static partial CMTime CMTimeMake(long value, int timescale);

        [LibraryImport(Path, EntryPoint = "CMTimeGetSeconds")]
        public static partial double CMTimeGetSeconds(CMTime time);

        [LibraryImport(Path, EntryPoint = "CMSampleBufferGetImageBuffer")]
        public static partial IntPtr CMSampleBufferGetImageBuffer(IntPtr sbuf);
        
        [LibraryImport(Path, EntryPoint = "CMSampleBufferGetDataBuffer")]
        public static partial IntPtr CMSampleBufferGetDataBuffer(IntPtr sbuf);
        
        [LibraryImport(Path, EntryPoint = "CMSampleBufferIsValid")]
        [return: MarshalAs(UnmanagedType.U1)]
        public static partial bool CMSampleBufferIsValid(IntPtr sbuf);
        
        [LibraryImport(Path, EntryPoint = "CMSampleBufferGetFormatDescription")]
        public static partial IntPtr CMSampleBufferGetFormatDescription(IntPtr sbuf);

        [LibraryImport(Path, EntryPoint = "CMSampleBufferGetDecodeTimeStamp")]
        public static partial CMTime CMSampleBufferGetDecodeTimeStamp(IntPtr sbuf);
        
        [LibraryImport(Path, EntryPoint = "CMBlockBufferGetDataPointer")]
        public static partial CMBlockBufferError CMBlockBufferGetDataPointer(IntPtr theBuffer, nuint offset, out IntPtr lengthAtOffset, out IntPtr totalLength, out IntPtr dataPointer);

        [LibraryImport(Path, EntryPoint = "CMBlockBufferGetDataLength")]
        public static partial nuint CMBlockBufferGetDataLength(IntPtr theBuffer);
        
        [LibraryImport(Path, EntryPoint = "CMFormatDescriptionGetMediaType")]
        public static partial CMMediaType CMFormatDescriptionGetMediaType(IntPtr desc);

        [LibraryImport(Path, EntryPoint = "CMSampleBufferGetSampleAttachmentsArray")]
        public static partial IntPtr CMSampleBufferGetSampleAttachmentsArray(IntPtr sampleBuffer, [MarshalAs(UnmanagedType.U1)] bool createIfNecessary);
        
        [LibraryImport(Path, EntryPoint = "CMFormatDescriptionGetMediaSubType")]
        public static partial uint CMFormatDescriptionGetMediaSubType(IntPtr desc);

        [LibraryImport(Path, EntryPoint = "CMVideoFormatDescriptionGetDimensions")]
        public static partial CMVideoDimensions CMVideoFormatDescriptionGetDimensions(IntPtr videoDesc);

        public enum CMMediaType : uint
        {
            Video = 1986618469, // 'vide'
        }

        public enum CMPixelFormat : uint
        {
            AlphaRedGreenBlue32bits = 32,
            BlueGreenRedAlpha32bits = 1111970369,
            RedGreenBlue24bits = 24,
            BigEndian555_16bits = 16,
            BigEndian565_16bits = 1110783541,
            LittleEndian555_16bits = 1278555445,
            LittleEndian565_16bits = 1278555701,
            LittleEndian5551_16bits = 892679473,
            YpCbCr422_8bits = 846624121,
            YpCbCr422yuvs_8bits = 2037741171,
            YpCbCr444_8bits = 1983066168,
            YpCbCrA4444_8bits = 1983131704,
            YpCbCr422_16bits = 1983000886,
            YpCbCr422_10bits = 1983000880,
            YpCbCr444_10bits = 1983131952,
            IndexedGrayWhiteIsZero_8bits = 40,
        }

        [StructLayout(LayoutKind.Sequential)]
        public partial struct CMTime
        {
            [Flags]
            public enum Flags : uint
            {
                Valid = 1,
                HasBeenRounded = 2,
                PositiveInfinity = 4,
                NegativeInfinity = 8,
                Indefinite = 16,
            }

            public long Value;
            public int TimeScale;
            public Flags TimeFlags;
            public long TimeEpoch;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CMVideoDimensions
        {
            public int Width;
            public int Height;
        }

        public sealed class CMFormatDescription : LibCoreFoundation.CFObject
        {
            public CMFormatDescription(IntPtr handle) :
                base(handle)
            { }

            public CMMediaType MediaType => CMFormatDescriptionGetMediaType(Handle);

            public CMVideoDimensions Dimensions => MediaType == CMMediaType.Video
                ? CMVideoFormatDescriptionGetDimensions(Handle)
                : default;
        }
    }

    public static partial class LibCoreVideo
    {
        private const string Path = "/System/Library/Frameworks/CoreVideo.framework/CoreVideo";
        
        private const int RTLD_NOW = 2;
        public static readonly IntPtr Handle = Dlfcn.dlopen(Path, RTLD_NOW);
        
        public static readonly IntPtr kCVPixelBufferPixelFormatTypeKey = Dlfcn.GetSymbolIndirect(Handle, "kCVPixelBufferPixelFormatTypeKey");

        public static readonly IntPtr kCVPixelBufferMetalCompatibilityKey = Dlfcn.GetSymbolIndirect(Handle, "kCVPixelBufferMetalCompatibilityKey");

        [LibraryImport(Path, EntryPoint = "CVPixelBufferGetDataSize")]
        public static partial nuint CVPixelBufferGetDataSize(IntPtr pixelBuffer);

        [LibraryImport(Path, EntryPoint = "CVPixelBufferGetPlaneCount")]
        public static partial nuint CVPixelBufferGetPlaneCount(IntPtr pixelBuffer);

        [LibraryImport(Path, EntryPoint = "CVPixelBufferGetBaseAddress")]
        public static partial IntPtr CVPixelBufferGetBaseAddress(IntPtr pixelBuffer);

        [LibraryImport(Path, EntryPoint = "CVPixelBufferLockBaseAddress")]
        public static partial int CVPixelBufferLockBaseAddress(IntPtr pixelBuffer, PixelBufferLockFlags lockFlags);

        [LibraryImport(Path, EntryPoint = "CVPixelBufferUnlockBaseAddress")]
        public static partial int CVPixelBufferUnlockBaseAddress(IntPtr pixelBuffer, PixelBufferLockFlags lockFlags);

        public enum PixelBufferLockFlags : long
        {
            None,
            ReadOnly = 1,
        }
        
        public static readonly int PixelFormatType_16LE555 = GetFourCC("L555");
        public static readonly int PixelFormatType_16LE5551 = GetFourCC("5551");
        public static readonly int PixelFormatType_16BE565 = GetFourCC("B565");
        public static readonly int PixelFormatType_16LE565 = GetFourCC("L565");
        public static readonly int PixelFormatType_24RGB = GetFourCC("24RG");
        public static readonly int PixelFormatType_24BGR = GetFourCC("24BG");
        public static readonly int PixelFormatType_32ARGB = GetFourCC("ARGB");
        public static readonly int PixelFormatType_32ABGR = GetFourCC("ABGR");
        public static readonly int PixelFormatType_32RGBA = GetFourCC("RGBA");
        public static readonly int PixelFormatType_32BGRA = GetFourCC("BGRA");
        public static readonly int PixelFormatType_64ARGB = GetFourCC("b64a");
        public static readonly int PixelFormatType_48RGB = GetFourCC("b48r");
        public static readonly int PixelFormatType_32AlphaGray = GetFourCC("b32a");
        public static readonly int PixelFormatType_16Gray = GetFourCC("b16g");
        public static readonly int PixelFormatType_30RGB = GetFourCC("R10k");
        public static readonly int PixelFormatType_422YpCbCr8 = GetFourCC("2vuy");
        public static readonly int PixelFormatType_4444YpCbCrA8 = GetFourCC("v408");
        public static readonly int PixelFormatType_4444YpCbCrA8R = GetFourCC("r408");
        public static readonly int PixelFormatType_4444AYpCbCr8 = GetFourCC("y408");
        public static readonly int PixelFormatType_4444AYpCbCr16 = GetFourCC("y416");
        public static readonly int PixelFormatType_444YpCbCr8 = GetFourCC("v308");
        public static readonly int PixelFormatType_422YpCbCr16 = GetFourCC("v216");
        public static readonly int PixelFormatType_422YpCbCr10 = GetFourCC("v210");
        public static readonly int PixelFormatType_444YpCbCr10 = GetFourCC("v410");
        public static readonly int PixelFormatType_422YpCbCr8_yuvs = GetFourCC("yuvs");
        public static readonly int PixelFormatType_422YpCbCr8FullRange = GetFourCC("yuvf");

        public static unsafe string GetFourCCName(int value)
        {
            var buffer = stackalloc char[4];

            buffer[0] = (char)(byte)(value >> 24);
            buffer[1] = (char)(byte)(value >> 16);
            buffer[2] = (char)(byte)(value >> 8);
            buffer[3] = (char)(byte)value;

            return new string(buffer);
        }
        
        private static int GetFourCC(string s)
        {
            var fcc = new FourCharCode(s);
            return fcc.GetIntVal();
        }

    }
    
    public abstract class NativeObject : IDisposable
    {
        ~NativeObject()
        {
            Dispose(false);
        }

        public IntPtr Handle { get; protected set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected abstract void Dispose(bool disposing);
    }
}
