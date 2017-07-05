using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        public const string SystemBusAddress = "unix:path=/var/run/dbus/system_bus_socket";

        private readonly IntPtr socketHandle;
        private readonly Stream stream;
        private readonly CancellationTokenSource receiveCts;
        private readonly Task receiveTask;

        private SemaphoreSlim semaphoreSend;
        private int serialCounter;
        private IOrgFreedesktopDbus orgFreedesktopDbus;

        private Connection(IntPtr socketHandle, Stream stream)
        {
            this.socketHandle = socketHandle;
            this.stream = stream;
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
            var endPoint = EndPointFactory.Create(options.Address);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
            await socket.ConnectAsync(endPoint).ConfigureAwait(false);
            var stream = new NetworkStream(socket, ownsSocket: true);

            await authenticate(stream).ConfigureAwait(false);

            var socketHandle = getSocketHandle(socket);
            var result = new Connection(socketHandle, stream);

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

        // TODO: Only support netstandard 2.0 and beyond
        private static IntPtr getSocketHandle(Socket socket)
        {
            // netstandard up until 1.6 doesn't provide the Handle property
            // or any other way to get the raw socket handle.
            // Use reflection...
            var type = socket.GetType().GetTypeInfo();
            var property = type.GetProperty("Handle");
            if (property != null)
                // ... to access the existing property on full framework...
                return (IntPtr)property.GetValue(socket);
            else
            {
                // ... or access the private(!) field of the .NET Core's implementation
                var field = type.GetField("_handle", BindingFlags.Instance | BindingFlags.NonPublic);
                return ((SafeHandle)field.GetValue(socket)).DangerousGetHandle();
            }
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

        private async Task serializedWriteToStream(byte[] messageArray)
        {
            await semaphoreSend.WaitAsync().ConfigureAwait(false);
            try
            {
                await stream.WriteAsync(messageArray, 0, messageArray.Length).ConfigureAwait(false);
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
            stream.Dispose();
            receiveTask.Wait();
        }

        private class deregistration : IDisposable
        {
            public Action Deregister;

            public void Dispose() => Deregister();
        }
    }
}
