using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

using NativeInt = System.Int64;

namespace Dbus
{
    public class SocketOperations64 : SocketOperationsBase, ISocketOperations
    {

        [DllImport("libc", SetLastError = true)]
        private static extern int connect(SafeHandle sockfd, [In] byte[] addr, NativeInt addrlen);

        public SocketOperations64(byte[] sockaddr)
            : base(sockaddr)
        {
            var connectResult = connect(Handle, sockaddr, sockaddr.Length);
            if (connectResult < 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public void WriteLine(string contents)
        {
            contents += Newline;
            var sendBytes = Encoding.ASCII.GetBytes(contents);
            Send(sendBytes);
        }

        [DllImport("libc", SetLastError = true)]
        private static extern unsafe NativeInt recv(SafeHandle sockfd, byte* buf, NativeInt len, int flags);
        public string ReadLine()
        {
            var line = "";
            var receiveByte = new byte[1];
            while (!line.EndsWith(Newline))
            {
                var result = Read(Handle, receiveByte, 0, 1);
                if (result != 1)
                    throw new InvalidOperationException("recv failed: " + result);
                line += Encoding.ASCII.GetString(receiveByte);
            }

            var toReturn = line.Substring(0, line.Length - Newline.Length);
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
        private static extern unsafe NativeInt send(SafeHandle sockfd, [In] byte* buf, NativeInt len, int flags);
        public void Send(byte[] messageArray)
        {
            var sendResult = Send(Handle, messageArray, messageArray.Length, 0);
            if (sendResult < 0)
                throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        public unsafe int Send(SafeHandle sockfd, byte[] buffer, int offset, int count)
        {
            fixed (byte* bufferP = buffer)
            {
                var sendResult = send(sockfd, bufferP + offset, count, 0);
                if (sendResult < 0)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return (int)sendResult;
            }
        }

        [DllImport("libc")]
        private static extern int recvmsg(SafeHandle sockfd, [In] ref msghdr buf, int flags);
        public unsafe void ReceiveMessage(
            byte* fixedLengthHeader, int fixedLengthHeaderLength,
            int* control, int controlLength
        )
        {
            const int iovecsLength = 1;
            var iovecs = stackalloc iovec[iovecsLength];

            iovecs[0].iov_base = fixedLengthHeader;
            iovecs[0].iov_len = fixedLengthHeaderLength;

            var msg = new msghdr
            {
                iov = iovecs,
                iovlen = 1,
                controllen = controlLength * sizeof(int),
                control = control
            };
            var length = recvmsg(Handle, ref msg, 0);
            if (length == 0)
                throw new OperationCanceledException("Socket was shut down");
            if (length < 0)
                throw new InvalidOperationException("recvmsg failed with " + length);
        }

        public unsafe bool ReceiveMessage(
            byte* header, int headerLength,
            byte* body, int bodyLength,
            byte* fixedLengthHeader, int fixedLengthHeaderLength,
            int* control, int controlLength
        )
        {
            const int iovecsLength = 3;
            var iovecs = stackalloc iovec[iovecsLength];

            iovecs[0].iov_base = header;
            iovecs[0].iov_len = headerLength;
            iovecs[1].iov_base = body;
            iovecs[1].iov_len = bodyLength;
            iovecs[2].iov_base = fixedLengthHeader;
            iovecs[2].iov_len = fixedLengthHeaderLength;
            var nextMsg = new msghdr
            {
                iov = iovecs,
                iovlen = iovecsLength,
                control = control,
                controllen = controlLength * sizeof(int),
            };
            var length = recvmsg(Handle, ref nextMsg, 0);
            if (length == 0)
                throw new OperationCanceledException("Socket was shut down");
            if (length < 0)
                throw new InvalidOperationException("recvmsg failed with " + length);
            return length == headerLength + bodyLength + fixedLengthHeaderLength;
        }

        private unsafe struct iovec
        {
            public byte* iov_base;
            public NativeInt iov_len;
        }

        private unsafe struct msghdr
        {
            public IntPtr name;
            public NativeInt namelen;
            public iovec* iov;
            public NativeInt iovlen;
            public int* control;
            public NativeInt controllen;
            public int flags;
        }
    }
}
