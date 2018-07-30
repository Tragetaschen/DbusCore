using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dbus
{
    internal class UnixFdStream : Stream
    {
        private readonly SafeHandle safeHandle;
        private readonly SocketOperations socketOperations;

        public UnixFdStream(SafeHandle safeHandle, SocketOperations socketOperations)
        {
            this.safeHandle = safeHandle;
            this.socketOperations = socketOperations;
            socketOperations.SetNonblocking(safeHandle);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get; set; }

        public override void Flush()
        { }

        public override unsafe int Read(byte[] buffer, int offset, int count) => socketOperations.Read(safeHandle, buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => 0;

        public override void SetLength(long value)
        { }

        public override void Write(byte[] buffer, int offset, int count) => socketOperations.Send(safeHandle, buffer, offset, count);

        protected override void Dispose(bool disposing)
        {
            if (safeHandle != null)
                safeHandle.Close();
            base.Dispose(disposing);
        }
    }
}
