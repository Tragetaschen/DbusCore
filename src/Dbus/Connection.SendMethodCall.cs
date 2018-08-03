using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ReceivedMessage>> expectedMessages =
            new ConcurrentDictionary<uint, TaskCompletionSource<ReceivedMessage>>();

        public async Task<ReceivedMessage> SendMethodCall(
            ObjectPath path,
            string interfaceName,
            string methodName,
            string destination,
            Encoder body,
            Signature signature
        )
        {
            var serial = getSerial();

            var bodySegments = await body.CompleteWritingAsync().ConfigureAwait(false);
            var bodyLength = 0;
            foreach (var segment in bodySegments)
                bodyLength += segment.Length;

            var header = new Encoder();
            header.Add((byte)DbusEndianess.LittleEndian);
            header.Add((byte)DbusMessageType.MethodCall);
            header.Add((byte)DbusMessageFlags.None);
            header.Add((byte)DbusProtocolVersion.Default);
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

            var headerSegments = await header.CompleteWritingAsync().ConfigureAwait(false);

            var tcs = new TaskCompletionSource<ReceivedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            expectedMessages[serial] = tcs;

            await serializedWriteToStream(headerSegments, bodySegments).ConfigureAwait(false);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);

            return await tcs.Task.ConfigureAwait(false);
        }

        private void handleMethodReturn(MessageHeader header, Decoder decoder)
        {
            if (!expectedMessages.TryRemove(header.ReplySerial, out var tcs))
            {
                decoder.Dispose();
                throw new InvalidOperationException("Couldn't find the method call for the method return");
            }

            var receivedMessage = new ReceivedMessage(header, decoder);
            tcs.SetResult(receivedMessage);
        }

        private void handleError(MessageHeader header, Decoder decoder)
        {
            using (decoder)
            {
                if (header.ReplySerial == 0)
                    throw new InvalidOperationException("Only errors for method calls are supported");
                if (!header.BodySignature.ToString().StartsWith("s"))
                    throw new InvalidOperationException("Errors are expected to start their body with a string");

                if (!expectedMessages.TryRemove(header.ReplySerial, out var tcs))
                    throw new InvalidOperationException("Couldn't find the method call for the error");

                var message = decoder.GetString();
                var exception = new DbusException(header.ErrorName, message);
                tcs.SetException(exception);
            }
        }
    }
}
