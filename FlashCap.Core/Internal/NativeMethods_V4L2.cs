////////////////////////////////////////////////////////////////////////////
//
// FlashCap - Independent camera capture library.
// Copyright (c) Kouji Matsui (@kekyo@mi.kekyo.net)
//
// Licensed under Apache-v2: https://opensource.org/licenses/Apache-2.0
//
////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using FlashCap.Internal.V4L2;
using FlashCap.Utilities;

using static FlashCap.Internal.V4L2.NativeMethods_V4L2_Interop;

namespace FlashCap.Internal;

[SupportedOSPlatform("linux")]
internal static partial class NativeMethods_V4L2
{
    public static readonly NativeMethods_V4L2_Interop Interop;

    private static readonly Dictionary<uint, PixelFormats> pixelFormats = new();

    static unsafe NativeMethods_V4L2()
    {
        utsname buf;
        while (uname(out buf) != 0)
        {
            var hr = Marshal.GetLastWin32Error();
            if (hr != EINTR)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        var machine = buf.GetMachine();
        switch (machine)
        {
            case "x86_64":
            case "amd64":
            case "i686":
            case "i586":
            case "i486":
            case "i386":
                Interop = IntPtr.Size == 8 ?
                    new NativeMethods_V4L2_Interop_x86_64() :
                    new NativeMethods_V4L2_Interop_i686();
                break;
            case "aarch64":
            case "armv9l":
            case "armv8l":
            case "armv7l":
            case "armv6l":
                Interop = IntPtr.Size == 8 ?
                    new NativeMethods_V4L2_Interop_aarch64() :
                    new NativeMethods_V4L2_Interop_armv7l();
                break;
            case "mips":
            case "mipsel":
                Interop = new NativeMethods_V4L2_Interop_mips();
                break;
            case "loongarch64":
                Interop = new NativeMethods_V4L2_Interop_loongarch64();
                break;
            default:
                throw new InvalidOperationException(
                    $"FlashCap: Architecture '{machine}' is not supported.");
        }

        pixelFormats.Add((uint)NativeMethods.Compression.BI_RGB, PixelFormats.RGB24);
        pixelFormats.Add((uint)NativeMethods.Compression.BI_JPEG, PixelFormats.JPEG);
        pixelFormats.Add((uint)NativeMethods.Compression.BI_PNG, PixelFormats.PNG);
        pixelFormats.Add((uint)NativeMethods.Compression.D3D_RGB24, PixelFormats.RGB24);
        pixelFormats.Add((uint)NativeMethods.Compression.D3D_RGB32, PixelFormats.RGB32);
        pixelFormats.Add((uint)NativeMethods.Compression.D3D_ARGB32, PixelFormats.ARGB32);
        pixelFormats.Add((uint)NativeMethods.Compression.D3D_RGB565, PixelFormats.RGB16);
        pixelFormats.Add((uint)NativeMethods.Compression.D3D_RGB555, PixelFormats.RGB15);
        pixelFormats.Add((uint)NativeMethods.Compression.RGB2, PixelFormats.RGB24);

        pixelFormats.Add(Interop.V4L2_PIX_FMT_RGB332, PixelFormats.RGB8);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_RGB565X, PixelFormats.RGB15);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_RGB565, PixelFormats.RGB16);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_RGB24, PixelFormats.RGB24);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_XRGB32, PixelFormats.RGB32);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_ABGR32, PixelFormats.ARGB32);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_ARGB, PixelFormats.ARGB32);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_MJPEG, PixelFormats.JPEG);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_JPEG, PixelFormats.JPEG);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_UYVY, PixelFormats.UYVY);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_YUYV, PixelFormats.YUYV);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_YUY2, PixelFormats.YUYV);
        pixelFormats.Add(Interop.V4L2_PIX_FMT_NV12, PixelFormats.NV12);
    }

    public static bool IsKnownPixelFormat(uint pix_fmt) =>
        pixelFormats.ContainsKey(pix_fmt);

    public const int EINTR = 4;
    public const int EINVAL = 22;

    [StructLayout(LayoutKind.Sequential)]
    public struct timeval
    {
        public IntPtr tv_sec;
        public IntPtr tv_usec;
    }

    [Flags]
    public enum OPENBITS
    {
        O_RDONLY = 0,
        O_WRONLY = 1,
        O_RDWR = 2,
    }

    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
    public static partial int open(
        string pathname, OPENBITS flag);

    [LibraryImport("libc", EntryPoint = "read", SetLastError = true)]
    private static unsafe partial int read(
        int fd, byte* buffer, int length);

    public static unsafe int read(
        int fd, byte[] buffer, int length)
    {
        fixed (byte* bufferPointer = buffer)
        {
            return read(fd, bufferPointer, length);
        }
    }

    [LibraryImport("libc", EntryPoint = "write", SetLastError = true)]
    private static unsafe partial int write(
        int fd, byte* buffer, int count);

    public static unsafe int write(
        int fd, byte[] buffer, int count)
    {
        fixed (byte* bufferPointer = buffer)
        {
            return write(fd, bufferPointer, count);
        }
    }

    [LibraryImport("libc", EntryPoint = "close", SetLastError = true)]
    public static partial int close(int fd);

    [LibraryImport("libc", EntryPoint = "pipe", SetLastError = true)]
    private static unsafe partial int pipe(int* filedes);

    public static unsafe int pipe(int[] filedes)
    {
        fixed (int* filedesPointer = filedes)
        {
            return pipe(filedesPointer);
        }
    }

    [Flags]
    public enum POLLBITS : short
    {
        POLLIN = 0x01,
        POLLPRI = 0x02,
        POLLOUT = 0x04,
        POLLERR = 0x08,
        POLLHUP = 0x10,
        POLLNVAL = 0x20,
        POLLRDNORM = 0x40,
        POLLRDBAND = 0x80,
        POLLWRNORM = 0x100,
        POLLWRBAND = 0x200,
        POLLMSG = 0x400,
        POLLREMOVE = 0x1000,
        POLLRDHUP = 0x2000,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct pollfd
    {
        public int fd;
        public POLLBITS events;
        public POLLBITS revents;
    }

    [LibraryImport("libc", EntryPoint = "poll", SetLastError = true)]
    private static unsafe partial int poll(
        pollfd* fds, int nfds, int timeout);

    public static unsafe int poll(
        pollfd[] fds, int nfds, int timeout)
    {
        fixed (pollfd* fdsPointer = fds)
        {
            return poll(fdsPointer, nfds, timeout);
        }
    }

    [Flags]
    public enum PROT
    {
        NONE = 0,
        READ = 1,
        WRITE = 2,
        EXEC = 4,
    }

    [Flags]
    public enum MAP
    {
        SHARED = 1,
        PRIVATE = 2,
    }

    public static readonly IntPtr MAP_FAILED = (IntPtr)(-1);

    [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
    private static partial IntPtr mmap3232(
        IntPtr addr, uint length, PROT prot, MAP flags, int fd, int offset);
    [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
    private static partial IntPtr mmap3264(
        IntPtr addr, uint length, PROT prot, MAP flags, int fd, long offset);
    [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
    private static partial IntPtr mmap6432(
        IntPtr addr, ulong length, PROT prot, MAP flags, int fd, int offset);
    [LibraryImport("libc", EntryPoint = "mmap", SetLastError = true)]
    private static partial IntPtr mmap6464(
        IntPtr addr, ulong length, PROT prot, MAP flags, int fd, long offset);

    public static IntPtr mmap(
        IntPtr addr, ulong length, PROT prot, MAP flags, int fd, long offset)
    {
        if (Interop.sizeof_size_t == 4)
        {
            if (Interop.sizeof_off_t == 4)
            {
                return mmap3232(addr, (uint)length, prot, flags, fd, (int)offset);
            }
            else
            {
                return mmap3264(addr, (uint)length, prot, flags, fd, offset);
            }
        }
        else
        {
            if (Interop.sizeof_off_t == 4)
            {
                return mmap6432(addr, length, prot, flags, fd, (int)offset);
            }
            else
            {
                return mmap6464(addr, length, prot, flags, fd, offset);
            }
        }
    }

    [LibraryImport("libc", EntryPoint = "munmap", SetLastError = true)]
    private static partial int munmap32(
        IntPtr addr, uint length);
    [LibraryImport("libc", EntryPoint = "munmap", SetLastError = true)]
    private static partial int munmap64(
        IntPtr addr, ulong length);

    public static int munmap(
        IntPtr addr, ulong length)
    {
        if (Interop.sizeof_size_t == 4)
        {
            return munmap32(addr, (uint)length);
        }
        else
        {
            return munmap64(addr, length);
        }
    }

    [LibraryImport("libc", EntryPoint = "ioctl", SetLastError = true)]
    private static partial int ioctl(
        int fd, UIntPtr request, IntPtr arg);

    public static int ioctl<T>(int fd, uint request, T arg)
    {
        var handle = GCHandle.Alloc(arg, GCHandleType.Pinned);
        try
        {
            while (true)
            {
                var result = ioctl(fd, (UIntPtr)request, handle.AddrOfPinnedObject());
                if (result < 0 && Marshal.GetLastWin32Error() == EINTR)
                {
                    continue;
                }

                return result;
            }
        }
        finally
        {
            handle.Free();
        }
    }

    private const int _UTSNAME_LENGTH = 65;

    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct utsname
    {
        public fixed byte sysname[_UTSNAME_LENGTH];
        public fixed byte nodename[_UTSNAME_LENGTH];
        public fixed byte release[_UTSNAME_LENGTH];
        public fixed byte version[_UTSNAME_LENGTH];
        public fixed byte machine[_UTSNAME_LENGTH];
        public fixed byte domainname[_UTSNAME_LENGTH];

        public string GetMachine()
        {
            fixed (byte* machinePointer = this.machine)
            {
                return Marshal.PtrToStringAnsi((IntPtr)machinePointer) ?? string.Empty;
            }
        }
    }

    [LibraryImport("libc", EntryPoint = "uname", SetLastError = true)]
    public static partial int uname(out utsname buf);
    
    ///////////////////////////////////////////////////////////

    public static VideoCharacteristics? CreateVideoCharacteristics(
        uint pix_fmt,
        int width, int height,
        Fraction framesPerSecond,
        string description,
        bool isDiscrete)
    {
        if (!pixelFormats.TryGetValue(pix_fmt, out var pixelFormat))
        {
            pixelFormat = PixelFormats.Unknown;
        }
        return new VideoCharacteristics(
            pixelFormat, width, height,
            framesPerSecond.Reduce(),
            description,
            isDiscrete,
            NativeMethods.GetFourCCString((int)pix_fmt));
    }

    public static uint[] GetPixelFormats(
        PixelFormats pixelFormat)
    {
        switch (pixelFormat)
        {
            case PixelFormats.RGB8:
                return new[] { Interop.V4L2_PIX_FMT_RGB332 };
            case PixelFormats.RGB15:
                return new[] { Interop.V4L2_PIX_FMT_RGB565X, (uint)NativeMethods.Compression.D3D_RGB555 };
            case PixelFormats.RGB16:
                return new[] { Interop.V4L2_PIX_FMT_RGB565, (uint)NativeMethods.Compression.D3D_RGB565 };
            case PixelFormats.RGB24:
                return new[] { Interop.V4L2_PIX_FMT_RGB24, (uint)NativeMethods.Compression.BI_RGB, (uint)NativeMethods.Compression.RGB2, (uint)NativeMethods.Compression.D3D_RGB24 };
            case PixelFormats.RGB32:
                return new[] { Interop.V4L2_PIX_FMT_XRGB32, (uint)NativeMethods.Compression.D3D_RGB32 };
            case PixelFormats.ARGB32:
                return new[] { Interop.V4L2_PIX_FMT_ARGB32, Interop.V4L2_PIX_FMT_ARGB, (uint)NativeMethods.Compression.D3D_ARGB32 };
            case PixelFormats.UYVY:
                return new[] { Interop.V4L2_PIX_FMT_UYVY };
            case PixelFormats.YUYV:
                return new[] { Interop.V4L2_PIX_FMT_YUYV, Interop.V4L2_PIX_FMT_YUY2 };
            case PixelFormats.NV12:
                return new[] { Interop.V4L2_PIX_FMT_NV12 };
            case PixelFormats.JPEG:
                return new[] { Interop.V4L2_PIX_FMT_MJPEG, Interop.V4L2_PIX_FMT_JPEG, (uint)NativeMethods.Compression.BI_JPEG };
            case PixelFormats.PNG:
                return new[] { (uint)NativeMethods.Compression.BI_PNG };
            default:
                return ArrayEx.Empty<uint>();
        }
    }
}
