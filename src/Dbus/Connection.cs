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

        public async Task<ReceivedMethodReturn> SendMethodCall(
            string path,
            string interfaceName,
            string methodName,
            string destination
        )
        {
            var serial = Interlocked.Increment(ref serialCounter);

            var header = new List<byte>();
            var index = 0;
            Encoder.Add(header, ref index, (byte)'l'); // little endian
            Encoder.Add(header, ref index, (byte)1); // method call
            Encoder.Add(header, ref index, (byte)0); // flags
            Encoder.Add(header, ref index, (byte)1); // protocol version
            Encoder.Add(header, ref index, 0); // body length
            Encoder.Add(header, ref index, serial); // serial

            Encoder.AddArray(header, ref index, (List<byte> buffer, ref int localIndex) =>
            {
                Encoder.EnsureAlignment(buffer, ref localIndex, 8);
                Encoder.Add(buffer, ref localIndex, (byte)1);
                Encoder.AddVariant(buffer, ref localIndex, path, isObjectPath: true);

                Encoder.EnsureAlignment(buffer, ref localIndex, 8);
                Encoder.Add(buffer, ref localIndex, (byte)2);
                Encoder.AddVariant(buffer, ref localIndex, interfaceName);

                Encoder.EnsureAlignment(buffer, ref localIndex, 8);
                Encoder.Add(buffer, ref localIndex, (byte)3);
                Encoder.AddVariant(buffer, ref localIndex, methodName);

                Encoder.EnsureAlignment(buffer, ref localIndex, 8);
                Encoder.Add(buffer, ref localIndex, (byte)6);
                Encoder.AddVariant(buffer, ref localIndex, destination);
            });
            Encoder.EnsureAlignment(header, ref index, 8);

            var tcs = new TaskCompletionSource<ReceivedMethodReturn>();
            expectedMessages[serial] = tcs;

            var headerArray = header.ToArray();
            await stream.WriteAsync(headerArray, 0, headerArray.Length).ConfigureAwait(false);

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
