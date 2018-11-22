using System;
using System.Collections.Concurrent;
using System.Threading;
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
            Signature signature,
            CancellationToken cancellationToken
        )
        {
            var serial = getSerial();

            var bodySegments = await body.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);
            var bodyLength = 0;
            foreach (var segment in bodySegments)
                bodyLength += segment.Length;

            var header = createHeader(
                DbusMessageType.MethodCall,
                DbusMessageFlags.None,
                bodyLength,
                e =>
                {
                    addHeader(e, path);
                    addHeader(e, DbusHeaderType.InterfaceName, interfaceName);
                    addHeader(e, DbusHeaderType.Member, methodName);
                    addHeader(e, DbusHeaderType.Destination, destination);
                    if (bodyLength > 0)
                        addHeader(e, signature);
                },
                serial
            );

            var headerSegments = await header.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

            var tcs = new TaskCompletionSource<ReceivedMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
            expectedMessages[serial] = tcs;

            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                await serializedWriteToStream(
                    headerSegments,
                    bodySegments,
                    cancellationToken
                ).ConfigureAwait(false);

                body.CompleteReading(bodySegments);
                header.CompleteReading(headerSegments);

                return await tcs.Task.ConfigureAwait(false);
            }
        }

        private void handleMethodReturn(MessageHeader header, Decoder decoder)
        {
            try
            {
                if (!expectedMessages.TryRemove(header.ReplySerial, out var tcs))
                {
                    decoder.Dispose();
                    throw new InvalidOperationException("Couldn't find the method call for the method return");
                }

                var receivedMessage = new ReceivedMessage(header, decoder);
                tcs.TrySetResult(receivedMessage);
            }
            catch(Exception e)
            {
                onUnobservedException(e);
            }
        }

        private void handleError(MessageHeader header, Decoder decoder)
        {
            try
            {
                using (decoder)
                {
                    if (header.ReplySerial == 0)
                        throw new InvalidOperationException("Only errors for method calls are supported");

                    if (!expectedMessages.TryRemove(header.ReplySerial, out var tcs))
                        throw new InvalidOperationException("Couldn't find the method call for the error");

                    var message = "";
                    if (header.BodySignature.ToString().StartsWith("s"))
                        message = decoder.GetString();

                    var exception = new DbusException(header.ErrorName, message);
                    tcs.TrySetException(exception);
                }
            }
            catch(Exception e)
            {
                onUnobservedException(e);
            }
        }
    }
}
