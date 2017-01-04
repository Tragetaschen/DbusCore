using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        private readonly CancellationTokenSource receiveCts;
        private readonly Socket socket;
        private readonly Stream stream;
        private int serialCounter;
        private static readonly Encoding encoding = Encoding.UTF8;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<ReceivedMethodReturn>> expectedMessages;
        private readonly ConcurrentDictionary<string, Action<MessageHeader, byte[]>> signalHandlers;

        private Connection()
        {
            expectedMessages = new ConcurrentDictionary<int, TaskCompletionSource<ReceivedMethodReturn>>();
            signalHandlers = new ConcurrentDictionary<string, Action<MessageHeader, byte[]>>();
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
            socket.Connect(new systemBusEndPoint());
            stream = new LoggingStream(new NetworkStream(socket));
            receiveCts = new CancellationTokenSource();
        }

        public async static Task<Connection> CreateAsync()
        {
            var result = new Connection();
            await authenticate(result.stream).ConfigureAwait(false);
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            // Ideally, there would be a DisposeAsync to properly await the receive task.
            // It's stopped properly, though
            Task.Run(result.receive);
#pragma warning restore CS4014
            return result;
        }

        public IDisposable RegisterSignalHandler(
            string path,
            string interfaceName,
            string member,
            Action<MessageHeader, byte[]> handler
        )
        {
            var dictionaryEntry = path + "\0" + interfaceName + "\0" + member;
            if (!signalHandlers.TryAdd(dictionaryEntry, handler))
                throw new InvalidOperationException("Attempted to register a signal handler twice");

            return new signalDeregistration
            {
                Deregister = () =>
                {
                    Action<MessageHeader, byte[]> _;
                    signalHandlers.TryRemove(dictionaryEntry, out _);
                }
            };
        }

        private int ensureAlignment(List<byte> message, int alignment)
        {
            var result = Alignment.Calculate(message.Count, alignment);
            for (var i = 0; i < result; ++i)
                message.Add(0);
            return result;
        }

        private int addStringVariant(List<byte> message, byte signature, string s)
        {
            message.Add(1); // signature length
            message.Add(signature);
            message.Add(0);
            var result = 3;
            result += ensureAlignment(message, 4);
            var bytes = encoding.GetBytes(s);
            message.AddRange(BitConverter.GetBytes(bytes.Length));
            result += 4;
            message.AddRange(bytes);
            result += bytes.Length;
            message.Add(0);
            ++result;
            return result;
        }

        public async Task<ReceivedMethodReturn> SendMethodCall(
            string path,
            string interfaceName,
            string methodName,
            string destination
        )
        {
            var serial = Interlocked.Increment(ref serialCounter);

            var header = new List<byte>();
            header.Add((byte)'l'); // little endian
            header.Add(1); // method call
            header.Add(0); // flags
            header.Add(1); // protocol version
            header.AddRange(BitConverter.GetBytes(0)); // body length
            header.AddRange(BitConverter.GetBytes(serial)); // serial

            var arrayLength = 0;
            header.AddRange(BitConverter.GetBytes(0)); // array length
            arrayLength += ensureAlignment(header, 8);
            header.Add(1); // path
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'o', path);
            arrayLength += ensureAlignment(header, 8);
            header.Add(2); // interface
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', interfaceName);
            arrayLength += ensureAlignment(header, 8);
            header.Add(3); // member
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', methodName);
            arrayLength += ensureAlignment(header, 8);
            header.Add(6); // destination
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', destination);
            ensureAlignment(header, 8); // final padding

            var realLength = BitConverter.GetBytes(arrayLength);
            header[12] = realLength[0];
            header[13] = realLength[1];
            header[14] = realLength[2];
            header[15] = realLength[3];

            var tcs = new TaskCompletionSource<ReceivedMethodReturn>();
            expectedMessages[serial] = tcs;

            var buffer = header.ToArray();
            await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        private async Task receive()
        {
            var fixedLengthHeader = new byte[16]; // header up until the array length
            var token = receiveCts.Token;
            while (!token.IsCancellationRequested)
            {
                await stream.ReadAsync(fixedLengthHeader, 0, fixedLengthHeader.Length, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                var index = 0;
                var endianess = Decoder.GetByte(fixedLengthHeader, ref index);
                if (endianess != (byte)'l')
                    throw new InvalidDataException("Wrong endianess");
                var messageType = Decoder.GetByte(fixedLengthHeader, ref index);
                Decoder.GetByte(fixedLengthHeader, ref index);
                var protocolVersion = Decoder.GetByte(fixedLengthHeader, ref index);
                if (protocolVersion != 1)
                    throw new InvalidDataException("Wrong protocol version");
                var bodyLength = Decoder.GetInt32(fixedLengthHeader, ref index);
                var receivedSerial = Decoder.GetInt32(fixedLengthHeader, ref index);
                var receivedArrayLength = Decoder.GetInt32(fixedLengthHeader, ref index);
                Alignment.Advance(ref receivedArrayLength, 8);
                var headerBytes = new byte[receivedArrayLength];
                await stream.ReadAsync(headerBytes, 0, headerBytes.Length, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var header = new MessageHeader(headerBytes);

                var body = new byte[bodyLength];
                await stream.ReadAsync(body, 0, body.Length, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();

                switch (messageType)
                {
                    case 2:
                        handleMethodReturn(header, body);
                        break;
                    case 4:
                        handleSignal(header, body);
                        break;
                }
            }
        }

        private void handleMethodReturn(
            MessageHeader header,
            byte[] body
        )
        {
            TaskCompletionSource<ReceivedMethodReturn> tcs;
            if (!expectedMessages.TryRemove(header.ReplySerial, out tcs))
                throw new InvalidOperationException("Couldn't find the method call for the method return");
            var receivedMessage = new ReceivedMethodReturn
            {
                Body = body,
                Signature = header.BodySignature,
            };

            tcs.SetResult(receivedMessage);
        }

        private void handleSignal(
            MessageHeader header,
            byte[] body
        )
        {
            var dictionaryEntry = header.Path + "\0" + header.InterfaceName + "\0" + header.Member;
            Action<MessageHeader, byte[]> handler;
            if (signalHandlers.TryGetValue(dictionaryEntry, out handler))
                Task.Factory.StartNew(() => handler(header, body));
        }

        public void Dispose()
        {
            receiveCts.Cancel();
            stream.Dispose();
            socket.Dispose();
        }

        private class systemBusEndPoint : EndPoint
        {
            public override SocketAddress Serialize()
            {
                var socketFile = encoding.GetBytes("/var/run/dbus/system_bus_socket");
                var result = new SocketAddress(AddressFamily.Unix, socketFile.Length + 2);
                for (var i = 0; i < socketFile.Length; ++i)
                    result[i + 2] = socketFile[i];
                return result;
            }
        }
        private class signalDeregistration : IDisposable
        {
            public Action Deregister;

            public void Dispose()
            {
                Deregister();
            }
        }
    }
}
