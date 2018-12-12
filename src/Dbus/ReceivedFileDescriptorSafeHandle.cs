using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Dbus
{
    internal sealed class ReceivedFileDescriptorSafeHandle : SafeHandleMinusOneIsInvalid
    {
        public ReceivedFileDescriptorSafeHandle()
            : base(true)
        { }

        public ReceivedFileDescriptorSafeHandle(int fd)
            : base(true)
            => SetHandle(new IntPtr(fd));

        protected override bool ReleaseHandle() => close(handle) == 0;

        [DllImport("libc")]
        private static extern int close(IntPtr handle);
    }
}
