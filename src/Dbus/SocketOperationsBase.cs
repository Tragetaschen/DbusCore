using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Dbus
{
    public class SocketOperationsBase : IDisposable
    {
        protected const string Newline = "\r\n";

        protected readonly SafeHandle Handle;

        [DllImport("libc")]
        private static extern int socket(int domain, int type, int protocol);
        public SocketOperationsBase(byte[] sockaddr)
        {
            var fd = socket((int)AddressFamily.Unix, (int)SocketType.Stream, 0);
            if (fd < 0)
                throw new InvalidOperationException("Opening the socket failed");
            Handle = new ReceivedFileDescriptorSafeHandle(fd);
        }

        [DllImport("libc")]
        private static extern int shutdown(SafeHandle sockfd, int how);
        public void Shutdown() => shutdown(Handle, 2);

        public void Dispose() => Handle.Dispose();

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
    }
}
