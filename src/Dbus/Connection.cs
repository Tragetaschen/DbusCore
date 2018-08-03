using System;
using System.Buffers;
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

        private Encoder createHeader(
            DbusMessageType type,
            DbusMessageFlags flags,
            int bodyLength,
            Action<Encoder> otherHeaders,
            uint? serial = null
        )
        {
            var header = new Encoder();

            header.Add((byte)DbusEndianess.LittleEndian);
            header.Add((byte)type);
            header.Add((byte)flags);
            header.Add((byte)DbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(serial ?? getSerial());

            header.AddArray(() => otherHeaders(header));
            header.EnsureAlignment(8);

            return header;
        }

        private static void addHeader(Encoder encoder, ObjectPath path)
        {
            encoder.EnsureAlignment(8);
            encoder.Add((byte)DbusHeaderType.Path);
            encoder.AddVariant(path);
        }

        private static void addHeader(Encoder encoder, uint replySerial)
        {
            encoder.EnsureAlignment(8);
            encoder.Add((byte)DbusHeaderType.ReplySerial);
            encoder.AddVariant(replySerial);
        }

        private static void addHeader(Encoder encoder, Signature signature)
        {
            encoder.EnsureAlignment(8);
            encoder.Add((byte)DbusHeaderType.Signature);
            encoder.AddVariant(signature);
        }

        private static void addHeader(Encoder encoder, DbusHeaderType type, string value)
        {
            encoder.EnsureAlignment(8);
            encoder.Add((byte)type);
            encoder.AddVariant(value);
        }

        private uint getSerial() => (uint)Interlocked.Increment(ref serialCounter);

        private static IDisposable deregisterVia(Action work)
            => new deregistration
            {
                Deregister = work,
            };

        private async Task serializedWriteToStream(ReadOnlySequence<byte> header, ReadOnlySequence<byte> body)
        {
            var numberOfSegments = 0;
            foreach (var _ in header)
                ++numberOfSegments;
            foreach (var _ in body)
                ++numberOfSegments;

            var segmentsOwnedMemory = fillSegments(header, body, numberOfSegments);
            await semaphoreSend.WaitAsync().ConfigureAwait(false);
            try
            {
                socketOperations.Send(segmentsOwnedMemory.Memory.Span, numberOfSegments);
            }
            finally
            {
                segmentsOwnedMemory.Dispose();
                semaphoreSend.Release();
            }
        }

        private static IMemoryOwner<ReadOnlyMemory<byte>> fillSegments(ReadOnlySequence<byte> header, ReadOnlySequence<byte> body, int numberOfSegments)
        {
            var segmentsOwnedMemory = MemoryPool<ReadOnlyMemory<byte>>.Shared.Rent(numberOfSegments);
            var segments = segmentsOwnedMemory.Memory.Span;
            var index = 0;
            foreach (var segment in header)
                segments[index++] = segment;
            foreach (var segment in body)
                segments[index++] = segment;
            return segmentsOwnedMemory;
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
