using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Dbus
{
    public class SocketOperationsBase : IDisposable
    {
        protected const string Newline = "\r\n";

        protected readonly int Handle;

        [DllImport("libc")]
        private static extern int socket(int domain, int type, int protocol);
        public SocketOperationsBase(byte[] sockaddr)
        {
            Handle = socket((int)AddressFamily.Unix, (int)SocketType.Stream, 0);
            if (Handle < 0)
                throw new InvalidOperationException("Opening the socket failed");
        }

        [DllImport("libc")]
        private static extern int shutdown(int sockfd, int how);
        public void Shutdown() => shutdown(Handle, 2);

        [DllImport("libc")]
        private static extern int close(int fd);
        public void Dispose() => close(Handle);

        [DllImport("libc")]
        private static extern int getuid();
        public int Uid => getuid();
    }
}
