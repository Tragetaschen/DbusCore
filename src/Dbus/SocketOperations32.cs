using System;
using System.Runtime.InteropServices;
using System.Text;

using NativeInt = System.Int32;

namespace Dbus
{
    public class SocketOperations32 : SocketOperationsBase, ISocketOperations
    {
        [DllImport("libc")]
        private static extern int connect(int sockfd, [In] byte[] addr, NativeInt addrlen);

        public SocketOperations32(byte[] sockaddr)
            : base(sockaddr)
        {
            var connectResult = connect(Handle, sockaddr, sockaddr.Length);
            if (connectResult < 0)
                throw new InvalidOperationException("Connecting the socket failed");
        }

        public void WriteLine(string contents)
        {
            contents += Newline;
            var sendBytes = Encoding.ASCII.GetBytes(contents);
            Send(sendBytes);
        }

        [DllImport("libc")]
        private static extern NativeInt recv(int sockfd, byte[] buf, NativeInt len, int flags);
        public string ReadLine()
        {
            var line = "";
            var receiveByte = new byte[1];
            while (!line.EndsWith(Newline))
            {
                var result = recv(Handle, receiveByte, 1, 0);
                if (result != 1)
                    throw new InvalidOperationException("recv failed: " + result);
                line += Encoding.ASCII.GetString(receiveByte);
            }

            var toReturn = line.Substring(0, line.Length - Newline.Length);
            return toReturn;
        }

        [DllImport("libc")]
        private static extern NativeInt send(int sockfd, [In] byte[] buf, NativeInt len, int flags);
        public void Send(byte[] messageArray)
        {
            var sendResult = send(Handle, messageArray, messageArray.Length, 0);
            if (sendResult < 0)
                throw new InvalidOperationException("Send failed");
        }

        [DllImport("libc")]
        private static extern int recvmsg(int sockfd, [In] ref msghdr buf, int flags);
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
