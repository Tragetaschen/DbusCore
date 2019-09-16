using System;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    internal sealed class UnixFdStream : Stream
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
        public override int Read(byte[] buffer, int offset, int count) => socketOperations.Read(safeHandle, buffer, offset, count);

        public override bool CanWrite => true;
        public override void Write(byte[] buffer, int offset, int count) => socketOperations.Write(safeHandle, buffer, offset, count);
        public override void Flush() { }

        // The Stream base class implementations of Begin*/End* and *Async
        // only allow exactly one asynchronous operation running at the same time
        // and block otherwise. Override those operations and burn ThreadPool threads,
        // otherwise for example reading and writing a Bluez Bluetooth socket
        // simultaneously doesn't work.
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.Run(() => Read(buffer, offset, count), cancellationToken);
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.Run(() => Write(buffer, offset, count), cancellationToken);
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            ReadAsync(buffer, offset, count).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception!.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(t.Result);

                callback?.Invoke(tcs.Task);
            });
            return tcs.Task;
        }
        public override int EndRead(IAsyncResult asyncResult)
        {
            try
            {
                return ((Task<int>)asyncResult).Result;
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                throw;
            }
        }
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object? state)
        {
            var tcs = new TaskCompletionSource<int>(state);
            WriteAsync(buffer, offset, count).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    tcs.TrySetException(t.Exception!.InnerExceptions);
                else if (t.IsCanceled)
                    tcs.TrySetCanceled();
                else
                    tcs.TrySetResult(0);

                callback?.Invoke(tcs.Task);
            });
            return tcs.Task;
        }
        public override void EndWrite(IAsyncResult asyncResult)
        {
            try
            {
                ((Task<int>)asyncResult).Wait();
            }
            catch (AggregateException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException!).Throw();
                throw;
            }
        }

        public override bool CanSeek => false;
        public override long Length => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();

        protected override void Dispose(bool disposing)
        {
            if (safeHandle != null)
                safeHandle.Close();
            base.Dispose(disposing);
        }
    }
}
