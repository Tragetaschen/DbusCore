using DotNetCross.NativeInts;
using System;
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
            Send(sendBytes);
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
        private static extern unsafe nint send(SafeHandle sockfd, [In] byte* buf, nint len, int flags);
        public void Send(byte[] messageArray)
        {
            var sendResult = Send(handle, messageArray, 0, messageArray.Length);
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
            var length = recvmsg(handle, ref msg, 0);
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
            var length = recvmsg(handle, ref nextMsg, 0);
            if (length == 0)
                throw new OperationCanceledException("Socket was shut down");
            if (length < 0)
                throw new InvalidOperationException("recvmsg failed with " + length);
            return length == headerLength + bodyLength + fixedLengthHeaderLength;
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
            public int* control;
            public nint controllen;
            public int flags;
        }
    }
}
