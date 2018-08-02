﻿using DotNetCross.NativeInts;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Dbus
{
    public class SocketOperations : IDisposable
    {
        private const string newline = "\r\n";

        private readonly SafeHandle handle;

        public SocketOperations(byte[] sockaddr)
        {
            var fd = socket((int)AddressFamily.Unix, (int)SocketType.Stream, 0);
            if (fd < 0)
                throw new InvalidOperationException("Opening the socket failed");
            handle = new ReceivedFileDescriptorSafeHandle(fd);

            var connectResult = connect(handle, sockaddr, sockaddr.Length);
            if (connectResult < 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        [DllImport("libc")]
        private static extern int socket(int domain, int type, int protocol);

        [DllImport("libc")]
        private static extern int shutdown(SafeHandle sockfd, int how);
        public void Shutdown() => shutdown(handle, 2);

        public void Dispose() => handle.Dispose();

        [DllImport("libc")]
        private static extern int getuid();
        public int Uid => getuid();

        [DllImport("libc")]
        private static extern int fcntl(SafeHandle sockfd, int cmd, int flags);
        [DllImport("libc")]
        private static extern int fcntl(SafeHandle sockfd, int cmd);
        public void SetNonblocking(SafeHandle sockfd)
        {
            var flags = fcntl(sockfd, 3/*f_getfl*/);
            flags &= ~0x800/*o_nonblock*/;
            fcntl(sockfd, 4/*f_setfl*/, flags);
        }

        [DllImport("libc", SetLastError = true)]
        private static extern int connect(SafeHandle sockfd, [In] byte[] addr, nint addrlen);

        public void WriteLine(string contents)
        {
            contents += newline;
            var sendBytes = Encoding.ASCII.GetBytes(contents);
            Send(handle, sendBytes, 0, sendBytes.Length);
        }

        [DllImport("libc", SetLastError = true)]
        private static extern unsafe nint recv(SafeHandle sockfd, byte* buf, nint len, int flags);
        public string ReadLine()
        {
            var line = "";
            var receiveByte = new byte[1];
            while (!line.EndsWith(newline))
            {
                var result = Read(handle, receiveByte, 0, 1);
                if (result != 1)
                    throw new InvalidOperationException("recv failed: " + result);
                line += Encoding.ASCII.GetString(receiveByte);
            }

            var toReturn = line.Substring(0, line.Length - newline.Length);
            return toReturn;
        }

        public unsafe int Read(SafeHandle sockfd, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufferP = buffer)
            {
                var readBytes = recv(sockfd, bufferP + offset, count, 0);
                if (readBytes >= 0)
                    return (int)readBytes;
                else
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
            }
        }

        [DllImport("libc", SetLastError = true)]
        private static extern unsafe nint sendmsg(SafeHandle sockfd, [In] ref msghdr buf, int flags);
        public unsafe void Send(Span<ReadOnlyMemory<byte>> segments, int numberOfSegments)
        {
            var handlesMemoryOwner= MemoryPool<MemoryHandle>.Shared.Rent(numberOfSegments);
            var handles = handlesMemoryOwner.Memory.Span;
            var iovecs = stackalloc iovec[numberOfSegments];

            for (var i = 0; i < numberOfSegments; ++i)
            {
                handles[i] = segments[i].Pin();
                iovecs[i].iov_base = (byte*)handles[i].Pointer;
                iovecs[i].iov_len = segments[i].Length;
            }

            var msg = new msghdr
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

        [DllImport("libc", SetLastError = true)]
        private static extern unsafe nint send(SafeHandle sockfd, [In] ref byte buf, nint len, int flags);

        public int Send(SafeHandle sockfd, byte[] buffer, int offset, int count)
        {
            var sendResult = send(sockfd, ref buffer[offset], count, 0);
            if (sendResult < 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
            return (int)sendResult;
        }

        [DllImport("libc")]
        private static extern int recvmsg(SafeHandle sockfd, [In] ref msghdr buf, int flags);
        public unsafe void ReceiveMessage(
            Span<byte> fixedLengthHeader,
            Span<byte> control
        )
        {
            fixed (byte* fixedLengthHeaderP = fixedLengthHeader)
            fixed (byte* controlP = control)
            {
                const int iovecsLength = 1;
                var iovecs = stackalloc iovec[iovecsLength];

                iovecs[0].iov_base = fixedLengthHeaderP;
                iovecs[0].iov_len = fixedLengthHeader.Length;

                var msg = new msghdr
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
                var iovecs = stackalloc iovec[iovecsLength];

                iovecs[0].iov_base = headerP;
                iovecs[0].iov_len = header.Length;
                iovecs[1].iov_base = bodyP;
                iovecs[1].iov_len = body.Length;
                iovecs[2].iov_base = fixedLengthHeaderP;
                iovecs[2].iov_len = fixedLengthHeader.Length;
                var nextMsg = new msghdr
                {
                    iov = iovecs,
                    iovlen = iovecsLength,
                    control = controlP,
                    controllen = control.Length,
                };
                var length = recvmsg(handle, ref nextMsg, 0);
                if (length == 0)
                    throw new OperationCanceledException("Socket was shut down");
                if (length < 0)
                    throw new InvalidOperationException("recvmsg failed with " + length);
                return length == header.Length + body.Length + fixedLengthHeader.Length;
            }
        }

        private unsafe struct iovec
        {
            public byte* iov_base;
            public nint iov_len;
        }

        private unsafe struct msghdr
        {
            public IntPtr name;
            public nint namelen;
            public iovec* iov;
            public nint iovlen;
            public byte* control;
            public nint controllen;
            public int flags;
        }
    }
}
