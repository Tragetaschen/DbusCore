using System;
using System.Buffers;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public sealed partial class Connection : IDisposable, IAsyncDisposable
    {
        public const string SystemBusAddress = "unix:path=/var/run/dbus/system_bus_socket";

        private readonly SocketOperations socketOperations;
        private readonly SemaphoreSlim semaphoreSend;
        private readonly CancellationTokenSource receiveCts;
        private readonly OrgFreedesktopDbus orgFreedesktopDbus;
        private readonly Task receiveTask;

        private int serialCounter;

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
            if (options.Address == null)
                throw new ArgumentException("No dbus address specified", nameof(options));
            var sockaddr = createSockaddr(options.Address);
            var socketOperations = new SocketOperations(sockaddr);

            await Task.Run(() => authenticate(socketOperations), cancellationToken).ConfigureAwait(false);

            var result = new Connection(socketOperations);
            await result.orgFreedesktopDbus.HelloAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }

        public static event EventHandler<UnobservedTaskExceptionEventArgs>? UnobservedException;
        private void onUnobservedException(Exception e)
        {
            if (Debugger.IsAttached)
                Debugger.Break();
            var eventArgs = new UnobservedTaskExceptionEventArgs(new AggregateException(e));
            UnobservedException?.Invoke(this, eventArgs);
            if (!eventArgs.Observed)
                Environment.FailFast("Unobserved exception in Dbus handling", e);
        }

        private static void standardHeaders(
            Encoder header,
            DbusMessageType type,
            DbusMessageFlags flags,
            int bodyLength,
            uint serial
        )
        {
            header.Add((byte)DbusEndianess.LittleEndian);
            header.Add((byte)type);
            header.Add((byte)flags);
            header.Add((byte)DbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(serial);
        }

        private static void addHeader(Encoder encoder, ObjectPath path)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)DbusHeaderType.Path);
            encoder.Add((Signature)"o");
            encoder.Add(path);
        }

        private static void addHeader(Encoder encoder, uint replySerial)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)DbusHeaderType.ReplySerial);
            encoder.Add((Signature)"u");
            encoder.Add(replySerial);
        }

        private static void addHeader(Encoder encoder, Signature signature)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)DbusHeaderType.Signature);
            encoder.Add((Signature)"g");
            encoder.Add(signature);
        }

        private static void addHeader(Encoder encoder, DbusHeaderType type, string value)
        {
            encoder.StartCompoundValue();
            encoder.Add((byte)type);
            encoder.Add((Signature)"s");
            encoder.Add(value);
        }

        private uint getSerial() => (uint)Interlocked.Increment(ref serialCounter);

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

        public void Dispose() => DisposeAsync().AsTask().GetAwaiter().GetResult();

        public async ValueTask DisposeAsync()
        {
            receiveCts.Cancel();
            socketOperations.Shutdown();
            try
            {
                await receiveTask;
            }
            catch (OperationCanceledException)
            { }
            socketOperations.Dispose();
            receiveCts.Dispose();
            semaphoreSend.Dispose();
        }
    }
}
