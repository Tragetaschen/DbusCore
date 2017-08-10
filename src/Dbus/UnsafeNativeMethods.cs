using System;
using System.Runtime.InteropServices;

using NativeInt = System.Int32;

namespace Dbus
{
    internal static class UnsafeNativeMethods
    {
        [DllImport("libc")]
        public static extern int socket(int domain, int type, int protocol);
        [DllImport("libc")]
        public static extern int connect(int sockfd, [In] byte[] addr, NativeInt addrlen);
        [DllImport("libc")]
        public static extern int send(int fd, [In] byte[] buf, int count, int flags);
        [DllImport("libc")]
        public static extern int recv(int sockfd, byte[] buf, NativeInt len, int flags);
        [DllImport("libc")]
        public static extern int recvmsg(int sockfd, [In] ref msghdr buf, int flags);
        [DllImport("libc")]
        public static extern int shutdown(int sockfd, int how);
        [DllImport("libc")]
        public static extern int close(int fd);
        [DllImport("libc")]
        public static extern int getuid();

        public unsafe struct iovec
        {
            public byte* iov_base;
            public NativeInt iov_len;
        }

        public unsafe struct msghdr
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
