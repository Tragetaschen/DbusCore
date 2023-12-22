using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Dbus;

public sealed partial class SocketOperations : IDisposable
{
    private const string newline = "\r\n";

    private readonly SafeHandle handle;

    public SocketOperations(byte[] sockaddr)
    {
        handle = socket((int)AddressFamily.Unix, (int)SocketType.Stream, 0);
        if (handle.IsInvalid)
            throw new Win32Exception();

        var connectResult = connect(handle, sockaddr, sockaddr.Length);
        if (connectResult < 0)
            throw new Win32Exception();
    }

    [LibraryImport("libc")]
    private static partial SafeFileHandle socket(int domain, int type, int protocol);

    [LibraryImport("libc")]
    private static partial int shutdown(SafeHandle sockfd, int how);
    public void Shutdown() => shutdown(handle, 2);

    public void Dispose() => handle.Dispose();

    [LibraryImport("libc")]
    private static partial int getuid();
    public int Uid => getuid();


    [LibraryImport("libc", SetLastError = true)]
    private static partial int connect(SafeHandle sockfd, [In] byte[] addr, nint addrlen);

    public void WriteLine(string contents)
    {
        contents += newline;
        var sendBytes = Encoding.ASCII.GetBytes(contents);
        Send(handle, sendBytes, 0, sendBytes.Length);
    }

    public string ReadLine()
    {
        var line = "";
        var receiveByte = new byte[1];
        while (!line.EndsWith(newline))
        {
            var result = Read(handle, receiveByte);
            if (result != 1)
                throw new InvalidOperationException("recv failed: " + result);
            line += Encoding.ASCII.GetString(receiveByte);
        }

        var toReturn = line.Substring(0, line.Length - newline.Length);
        return toReturn;
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial nint read(SafeHandle sockfd, ReadOnlySpan<byte> buf, nint len);
    public int Read(SafeHandle sockfd, ReadOnlySpan<byte> buffer)
    {
        var readBytes = read(sockfd, buffer, buffer.Length);
        if (readBytes >= 0)
            return (int)readBytes;
        else
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial nint sendmsg(SafeHandle sockfd, ref Msghdr buf, int flags);
    public unsafe void Send(Span<ReadOnlyMemory<byte>> segments, int numberOfSegments)
    {
        var handlesMemoryOwner = MemoryPool<MemoryHandle>.Shared.Rent(numberOfSegments);
        var handles = handlesMemoryOwner.Memory.Span;
        var iovecs = stackalloc Iovec[numberOfSegments];

        for (var i = 0; i < numberOfSegments; ++i)
        {
            handles[i] = segments[i].Pin();
            iovecs[i].iov_base = (byte*)handles[i].Pointer;
            iovecs[i].iov_len = segments[i].Length;
        }

        var msg = new Msghdr
        {
            iov = iovecs,
            iovlen = numberOfSegments,
        };

        var sendResult = sendmsg(handle, ref msg, 0);

        for (var i = 0; i < numberOfSegments; ++i)
            handles[i].Dispose();
        handlesMemoryOwner.Dispose();

        if (sendResult < 0)
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    [LibraryImport("libc", SetLastError = true)]
    private static partial nint send(SafeHandle sockfd, ref byte buf, nint len, int flags);

    public int Send(SafeHandle sockfd, byte[] buffer, int offset, int count)
    {
        var sendResult = send(sockfd, ref buffer[offset], count, 0);
        if (sendResult < 0)
            throw new Win32Exception();
        return (int)sendResult;
    }

    [LibraryImport("libc")]
    private static partial int recvmsg(SafeHandle sockfd, ref Msghdr buf, int flags);
    public unsafe void ReceiveMessage(
        Span<byte> fixedLengthHeader,
        Span<byte> control
    )
    {
        fixed (byte* fixedLengthHeaderP = fixedLengthHeader)
        fixed (byte* controlP = control)
        {
            const int iovecsLength = 1;
            var iovecs = stackalloc Iovec[iovecsLength];

            iovecs[0].iov_base = fixedLengthHeaderP;
            iovecs[0].iov_len = fixedLengthHeader.Length;

            var msg = new Msghdr
            {
                iov = iovecs,
                iovlen = 1,
                controllen = control.Length,
                control = controlP
            };
            var length = recvmsg(handle, ref msg, 0);
            if (length == 0)
                throw new OperationCanceledException("Socket was shut down");
            if (length < 0)
                throw new InvalidOperationException("recvmsg failed with " + length);
        }
    }

    public unsafe bool ReceiveMessage(
        Span<byte> header,
        Span<byte> body,
        Span<byte> fixedLengthHeader,
        Span<byte> control
    )
    {
        fixed (byte* headerP = header)
        fixed (byte* bodyP = body)
        fixed (byte* fixedLengthHeaderP = fixedLengthHeader)
        fixed (byte* controlP = control)
        {
            const int iovecsLength = 3;
            var iovecs = stackalloc Iovec[iovecsLength];

            var lengthTillBodyEnd = header.Length + body.Length;
            var allReceivedLength = 0;

            iovecs[0].iov_base = headerP;
            iovecs[0].iov_len = header.Length;
            iovecs[1].iov_base = bodyP;
            iovecs[1].iov_len = body.Length;
            iovecs[2].iov_base = fixedLengthHeaderP;
            iovecs[2].iov_len = fixedLengthHeader.Length;
            var nextMsg = new Msghdr
            {
                iov = iovecs,
                iovlen = iovecsLength,
                control = controlP,
                controllen = control.Length,
            };

            allReceivedLength += receiveAndCheck(ref nextMsg);

            if (allReceivedLength < lengthTillBodyEnd)
            {
                // Did not receive an entire body, continue by moving the contents
                // in the iovecs "up"…
                iovecs[1].iov_base = iovecs[2].iov_base; // and the new header
                iovecs[1].iov_len = iovecs[2].iov_len;
                nextMsg.iovlen -= 1;
            }
            while (allReceivedLength < lengthTillBodyEnd)
            {
                // …and by adapting the pointer and length of the body
                var offset = allReceivedLength - header.Length;
                iovecs[0].iov_base = bodyP + offset;
                iovecs[0].iov_len = lengthTillBodyEnd - allReceivedLength;

                allReceivedLength += receiveAndCheck(ref nextMsg);
            }

            return allReceivedLength == header.Length + body.Length + fixedLengthHeader.Length;
        }
    }

    private int receiveAndCheck(ref Msghdr nextMsg)
    {
        var length = recvmsg(handle, ref nextMsg, 0);
        if (length == 0)
            throw new OperationCanceledException("Socket was shut down");
        if (length < 0)
            throw new InvalidOperationException("recvmsg failed with " + length);
        return length;
    }

    private unsafe struct Iovec
    {
        public byte* iov_base;
        public nint iov_len;
    }

    private unsafe struct Msghdr
    {
        public IntPtr name;
        public nint namelen;
        public Iovec* iov;
        public nint iovlen;
        public byte* control;
        public nint controllen;
        public int flags;
    }
}
