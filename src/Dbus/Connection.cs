using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        public const string SystemBusAddress = "unix:path=/var/run/dbus/system_bus_socket";

        private readonly CancellationTokenSource receiveCts;
        private readonly Task receiveTask;

        private SemaphoreSlim semaphoreSend;
        private int serialCounter;
        private IOrgFreedesktopDbus orgFreedesktopDbus;

        private readonly SocketOperations socketOperations;

        private Connection(SocketOperations socketOperations)
        {
            this.socketOperations = socketOperations;

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
            var socketOperations = new SocketOperations(sockaddr);

            await Task.Run(() => authenticate(socketOperations)).ConfigureAwait(false);

            var result = new Connection(socketOperations);

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

        private uint getSerial() => (uint)Interlocked.Increment(ref serialCounter);

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
                socketOperations.Send(messageArray);
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
            socketOperations.Shutdown();
            try
            {
                receiveTask.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            { }
            socketOperations.Dispose();
        }

        private class deregistration : IDisposable
        {
            public Action Deregister;

            public void Dispose() => Deregister();
        }
    }
}
