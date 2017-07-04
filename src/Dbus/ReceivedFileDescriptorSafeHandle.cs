using System;
using System.Runtime.InteropServices;

namespace Dbus
{
    internal sealed class ReceivedFileDescriptorSafeHandle : SafeHandle
    {
        public ReceivedFileDescriptorSafeHandle(int handle)
            : base(IntPtr.Zero, true)
            => SetHandle(new IntPtr(handle));

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle() => close(handle) == 0;

        [DllImport("libc")]
        private static extern int close(IntPtr handle);
    }
}
