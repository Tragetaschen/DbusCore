using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ReceivedMethodReturn>> expectedMessages =
            new ConcurrentDictionary<uint, TaskCompletionSource<ReceivedMethodReturn>>();

        public async Task<ReceivedMethodReturn> SendMethodCall(
            ObjectPath path,
            string interfaceName,
            string methodName,
            string destination,
            Encoder body,
            Signature signature
        )
        {
            var serial = getSerial();

            var bodySegments = await body.FinishAsync().ConfigureAwait(false);
            var bodyLength = 0;
            foreach (var segment in bodySegments)
                bodyLength += segment.Length;

            var header = new Encoder();
            header.Add((byte)dbusEndianess.LittleEndian);
            header.Add((byte)dbusMessageType.MethodCall);
            header.Add((byte)dbusFlags.None);
            header.Add((byte)dbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(serial);

            header.AddArray(() =>
            {
                addHeader(header, path);
                addHeader(header, 2, interfaceName);
                addHeader(header, 3, methodName);
                addHeader(header, 6, destination);
                if (bodyLength > 0)
                    addHeader(header, signature);
            });
            header.EnsureAlignment(8);

            var headerSegments = await header.FinishAsync().ConfigureAwait(false);

            var tcs = new TaskCompletionSource<ReceivedMethodReturn>(TaskCreationOptions.RunContinuationsAsynchronously);
            expectedMessages[serial] = tcs;

            await serializedWriteToStream(headerSegments, bodySegments).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        private void handleMethodReturn(
            MessageHeader header,
            IMemoryOwner<byte> body,
            int bodyLength
        )
        {
            if (!expectedMessages.TryRemove(header.ReplySerial, out var tcs))
                throw new InvalidOperationException("Couldn't find the method call for the method return");
            var receivedMessage = new ReceivedMethodReturn
            {
                Header = header,
                Body = body,
                BodyLength = bodyLength,
                Signature = header.BodySignature,
            };

            tcs.SetResult(receivedMessage);
        }

        private void handleError(MessageHeader header, IMemoryOwner<byte> body, int bodyLength)
        {
            if (header.ReplySerial == 0)
                throw new InvalidOperationException("Only errors for method calls are supported");
            if (!header.BodySignature.ToString().StartsWith("s"))
                throw new InvalidOperationException("Errors are expected to start their body with a string");

            if (!expectedMessages.TryRemove(header.ReplySerial, out var tcs))
                throw new InvalidOperationException("Couldn't find the method call for the error");

            var index = 0;
            var bodyBytes = body.Limit(bodyLength);
            var message = Decoder.GetString(bodyBytes, ref index);
            body.Dispose();
            var exception = new DbusException(header.ErrorName, message);
            tcs.SetException(exception);
        }
    }
}
