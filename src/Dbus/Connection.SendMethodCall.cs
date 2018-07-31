using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            List<byte> body,
            Signature signature
        )
        {
            var serial = getSerial();

            var message = Encoder.StartNew();
            var index = 0;
            Encoder.Add(message, ref index, (byte)'l'); // little endian
            Encoder.Add(message, ref index, (byte)1); // method call
            Encoder.Add(message, ref index, (byte)0); // flags
            Encoder.Add(message, ref index, (byte)1); // protocol version
            Encoder.Add(message, ref index, body.Count); // Actually uint
            Encoder.Add(message, ref index, serial); // Actually uint

            Encoder.AddArray(message, ref index, (List<byte> buffer, ref int localIndex) =>
            {
                addHeader(buffer, ref localIndex, path);
                addHeader(buffer, ref localIndex, 2, interfaceName);
                addHeader(buffer, ref localIndex, 3, methodName);
                addHeader(buffer, ref localIndex, 6, destination);
                if (body.Count > 0)
                    addHeader(buffer, ref localIndex, signature);
            });
            Encoder.EnsureAlignment(message, ref index, 8);
            message.AddRange(body);

            var tcs = new TaskCompletionSource<ReceivedMethodReturn>(); //!! TaskCreationOptions.RunContinuationsAsynchronously
            expectedMessages[(uint)serial] = tcs;

            var messageArray = message.ToArray();
            await serializedWriteToStream(messageArray).ConfigureAwait(false);

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

            Task.Run(() => tcs.SetResult(receivedMessage));
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
            Task.Run(() => tcs.SetException(exception));
        }
    }
}
