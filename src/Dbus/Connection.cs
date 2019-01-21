using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        public const string SystemBusAddress = "unix:path=/var/run/dbus/system_bus_socket";

        private readonly CancellationTokenSource receiveCts;
        private readonly Task receiveTask;
        private readonly OrgFreedesktopDbus orgFreedesktopDbus;

        private SemaphoreSlim semaphoreSend;
        private int serialCounter;

        private readonly bool isMonoRuntime = Type.GetType("Mono.Runtime") != null;
        private readonly SocketOperations socketOperations;

        private Connection(SocketOperations socketOperations)
        {
            this.socketOperations = socketOperations;

            semaphoreSend = new SemaphoreSlim(1);
            receiveCts = new CancellationTokenSource();
            orgFreedesktopDbus = new OrgFreedesktopDbus(this);

            receiveTask = Task.Factory.StartNew(
                receive,
                receiveCts.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }

        public async static Task<Connection> CreateAsync(
            DbusConnectionOptions options,
            CancellationToken cancellationToken = default
        )
        {
            var sockaddr = createSockaddr(options.Address);
            var socketOperations = new SocketOperations(sockaddr);

            await Task.Run(() => authenticate(socketOperations), cancellationToken).ConfigureAwait(false);

            var result = new Connection(socketOperations);
            await result.orgFreedesktopDbus.HelloAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }

        public static event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedException;
        private void onUnobservedException(Exception e)
        {
            var eventArgs = new UnobservedTaskExceptionEventArgs(new AggregateException(e));
            UnobservedException?.Invoke(this, eventArgs);
            if (!eventArgs.Observed)
                Environment.FailFast("Unobserved exception in Dbus handling", e);
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

            header.AddArray(() => otherHeaders(header), storesCompoundValues: false);
            header.FinishHeader();

            return header;
        }

        private static void addHeader(Encoder encoder, ObjectPath path)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)DbusHeaderType.Path);
            encoder.AddVariant(path);
        }

        private static void addHeader(Encoder encoder, uint replySerial)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)DbusHeaderType.ReplySerial);
            encoder.AddVariant(replySerial);
        }

        private static void addHeader(Encoder encoder, Signature signature)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)DbusHeaderType.Signature);
            encoder.AddVariant(signature);
        }

        private static void addHeader(Encoder encoder, DbusHeaderType type, string value)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)type);
            encoder.AddVariant(value);
        }

        private uint getSerial() => (uint)Interlocked.Increment(ref serialCounter);

        private static IDisposable deregisterVia(Action work)
            => new deregistration
            {
                Deregister = work,
            };

        private async Task serializedWriteToStream(
            ReadOnlySequence<byte> header,
            ReadOnlySequence<byte> body,
            CancellationToken cancellationToken
        )
        {
            var numberOfSegments = 0;
            foreach (var _ in header)
                ++numberOfSegments;
            foreach (var _ in body)
                ++numberOfSegments;

            var segmentsOwnedMemory = fillSegments(header, body, numberOfSegments);
            await semaphoreSend.WaitAsync(cancellationToken).ConfigureAwait(false);
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
