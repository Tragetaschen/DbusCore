using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        public const string SystemBusAddress = "unix:path=/var/run/dbus/system_bus_socket";

        private readonly int socketHandle;
        private readonly CancellationTokenSource receiveCts;
        private readonly Task receiveTask;

        private SemaphoreSlim semaphoreSend;
        private int serialCounter;
        private IOrgFreedesktopDbus orgFreedesktopDbus;

        private Connection(int socketHandle)
        {
            this.socketHandle = socketHandle;
            semaphoreSend = new SemaphoreSlim(1);
            receiveCts = new CancellationTokenSource();

            receiveTask = Task.Factory.StartNew(
                receive,
                receiveCts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }

        public async static Task<Connection> CreateAsync(DbusConnectionOptions options)
        {
            var sockaddr = createSockaddr(options.Address);
            var socketHandle = UnsafeNativeMethods.socket((int)AddressFamily.Unix, (int)SocketType.Stream, 0);
            if (socketHandle < 0)
                throw new InvalidOperationException("Opening the socket failed");
            var connectResult = UnsafeNativeMethods.connect(socketHandle, sockaddr, sockaddr.Length);
            if (connectResult < 0)
                throw new InvalidOperationException("Connecting the socket failed");

            await Task.Run(() => authenticate(socketHandle)).ConfigureAwait(false);

            var result = new Connection(socketHandle);

            try
            {
                var orgFreedesktopDbus = result.Consume<IOrgFreedesktopDbus>();
                result.orgFreedesktopDbus = orgFreedesktopDbus;
                await orgFreedesktopDbus.HelloAsync().ConfigureAwait(false);
            }
            catch (KeyNotFoundException)
            {
                throw new InvalidOperationException("Could not find the generated implementation of 'IOrgFreedesktopDbus'. Did you run the DoInit method of the generated code?");
            }

            return result;
        }

        private static void addHeader(List<byte> buffer, ref int index, ObjectPath path)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, (byte)1);
            Encoder.AddVariant(buffer, ref index, path);
        }

        private static void addHeader(List<byte> buffer, ref int index, uint replySerial)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, (byte)5);
            Encoder.AddVariant(buffer, ref index, replySerial);
        }

        private static void addHeader(List<byte> buffer, ref int index, Signature signature)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, (byte)8);
            Encoder.AddVariant(buffer, ref index, signature);
        }

        private static void addHeader(List<byte> buffer, ref int index, byte type, string value)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, type);
            Encoder.AddVariant(buffer, ref index, value);
        }

        private int getSerial() => Interlocked.Increment(ref serialCounter);

        private static IDisposable deregisterVia(Action work)
            => new deregistration
            {
                Deregister = work,
            };

        private async Task serializedWriteToStream(byte[] messageArray)
        {
            await semaphoreSend.WaitAsync().ConfigureAwait(false);
            try
            {
                var sendResult = UnsafeNativeMethods.send(socketHandle, messageArray, messageArray.Length, 0);
                if (sendResult < 0)
                    throw new InvalidOperationException("Send failed");
            }
            finally
            {
                semaphoreSend.Release();
            }
        }

        public void Dispose()
        {
            orgFreedesktopDbus.Dispose();
            receiveCts.Cancel();
            UnsafeNativeMethods.shutdown(socketHandle, 2);
            receiveTask.Wait();
            UnsafeNativeMethods.close(socketHandle);
        }

        private class deregistration : IDisposable
        {
            public Action Deregister;

            public void Dispose() => Deregister();
        }
    }
}
