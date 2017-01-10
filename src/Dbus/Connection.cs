using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        public const string SystemBusAddress = "unix:path=/var/run/dbus/system_bus_socket";

        private readonly Stream stream;
        private readonly ConcurrentDictionary<uint, TaskCompletionSource<ReceivedMethodReturn>> expectedMessages;
        private readonly ConcurrentDictionary<string, Action<MessageHeader, byte[]>> signalHandlers;
        private readonly ConcurrentDictionary<string, Func<uint, MessageHeader, byte[], Task>> objectProxies;
        private readonly CancellationTokenSource receiveCts;

        private int serialCounter;

        private Connection(Stream stream)
        {
            this.stream = stream;

            expectedMessages = new ConcurrentDictionary<uint, TaskCompletionSource<ReceivedMethodReturn>>();
            signalHandlers = new ConcurrentDictionary<string, Action<MessageHeader, byte[]>>();
            objectProxies = new ConcurrentDictionary<string, Func<uint, MessageHeader, byte[], Task>>();
            receiveCts = new CancellationTokenSource();

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            // Ideally, there would be a DisposeAsync to properly await the receive task.
            // It's stopped properly, though
            Task.Run(receive);
#pragma warning restore CS4014
        }

        public async static Task<Connection> CreateAsync(DbusConnectionOptions options)
        {
            var endPoint = EndPointFactory.Create(options.Address);
            var socket = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
            await socket.ConnectAsync(endPoint).ConfigureAwait(false);
            var stream = new NetworkStream(socket, ownsSocket: true);

            await authenticate(stream).ConfigureAwait(false);

            var result = new Connection(stream);

            try
            {
                using (var orgFreedesktopDbus = result.Consume<IOrgFreedesktopDbus>())
                    await orgFreedesktopDbus.HelloAsync();
            }
            catch(KeyNotFoundException)
            {
                throw new InvalidOperationException("Could not find the generated implementation of 'IOrgFreedesktopDbus'. Did you run the DoInit method of the generated code?");
            }

            return result;
        }

        public IDisposable RegisterObjectProxy(
            ObjectPath path,
            string interfaceName,
            Func<uint, MessageHeader, byte[], Task> proxy
        )
        {
            var dictionaryEntry = path + "\0" + interfaceName;
            if (!objectProxies.TryAdd(dictionaryEntry, proxy))
                throw new InvalidOperationException("Attempted to register an object proxy twice");

            return new deregistration
            {
                Deregister = () =>
                {
                    Func<uint, MessageHeader, byte[], Task> _;
                    objectProxies.TryRemove(dictionaryEntry, out _);
                }
            };
        }

        public IDisposable RegisterSignalHandler(
            ObjectPath path,
            string interfaceName,
            string member,
            Action<MessageHeader, byte[]> handler
        )
        {
            var dictionaryEntry = path + "\0" + interfaceName + "\0" + member;
            signalHandlers.AddOrUpdate(
                dictionaryEntry,
                handler,
                (_, existingHandler) => existingHandler + handler
            );

            return new deregistration
            {
                Deregister = () =>
                {
                    Action<MessageHeader, byte[]> current;
                    do
                    {
                        signalHandlers.TryGetValue(dictionaryEntry, out current);
                    } while (!signalHandlers.TryUpdate(dictionaryEntry, current - handler, current));
                }
            };
        }

        private static void AddHeader(List<byte> buffer, ref int index, ObjectPath path)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, (byte)1);
            Encoder.AddVariant(buffer, ref index, path);
        }

        private static void AddHeader(List<byte> buffer, ref int index, uint replySerial)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, (byte)5);
            Encoder.AddVariant(buffer, ref index, replySerial);
        }

        private static void AddHeader(List<byte> buffer, ref int index, Signature signature)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, (byte)8);
            Encoder.AddVariant(buffer, ref index, signature);
        }

        private static void AddHeader(List<byte> buffer, ref int index, byte type, string value)
        {
            Encoder.EnsureAlignment(buffer, ref index, 8);
            Encoder.Add(buffer, ref index, type);
            Encoder.AddVariant(buffer, ref index, value);
        }

        public async Task<ReceivedMethodReturn> SendMethodCall(
            ObjectPath path,
            string interfaceName,
            string methodName,
            string destination,
            List<byte> body,
            Signature signature
        )
        {
            var serial = Interlocked.Increment(ref serialCounter);

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
                AddHeader(buffer, ref localIndex, path);
                AddHeader(buffer, ref localIndex, 2, interfaceName);
                AddHeader(buffer, ref localIndex, 3, methodName);
                AddHeader(buffer, ref localIndex, 6, destination);
                if (body.Count > 0)
                    AddHeader(buffer, ref localIndex, signature);
            });
            Encoder.EnsureAlignment(message, ref index, 8);
            message.AddRange(body);

            var tcs = new TaskCompletionSource<ReceivedMethodReturn>();
            expectedMessages[(uint)serial] = tcs;

            var messageArray = message.ToArray();
            await stream.WriteAsync(messageArray, 0, messageArray.Length).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        public async Task SendMethodReturnAsync(uint replySerial, string destination, List<byte> body, Signature signature)
        {
            var serial = Interlocked.Increment(ref serialCounter);

            var message = Encoder.StartNew();
            var index = 0;
            Encoder.Add(message, ref index, (byte)'l'); // little endian
            Encoder.Add(message, ref index, (byte)2); // method return
            Encoder.Add(message, ref index, (byte)1); // no reply expected
            Encoder.Add(message, ref index, (byte)1); // protocol version
            Encoder.Add(message, ref index, body.Count); // Actually uint
            Encoder.Add(message, ref index, serial); // Actually uint

            Encoder.AddArray(message, ref index, (List<byte> buffer, ref int localIndex) =>
            {
                AddHeader(buffer, ref localIndex, 6, destination);
                AddHeader(buffer, ref localIndex, replySerial);
                if (body.Count > 0)
                    AddHeader(buffer, ref localIndex, signature);
            });
            Encoder.EnsureAlignment(message, ref index, 8);
            message.AddRange(body);

            var messageArray = message.ToArray();
            await stream.WriteAsync(messageArray, 0, messageArray.Length).ConfigureAwait(false);
        }

        private async Task sendMethodCallErrorAsync(uint replySerial, string destination, string error, string errorMessage)
        {
            var serial = Interlocked.Increment(ref serialCounter);

            var index = 0;
            var body = Encoder.StartNew();
            Encoder.Add(body, ref index, errorMessage);

            var message = Encoder.StartNew();
            index = 0;
            Encoder.Add(message, ref index, (byte)'l'); // little endian
            Encoder.Add(message, ref index, (byte)3); // error
            Encoder.Add(message, ref index, (byte)1); // no reply expected
            Encoder.Add(message, ref index, (byte)1); // protocol version
            Encoder.Add(message, ref index, body.Count); // Actually uint
            Encoder.Add(message, ref index, serial); // Actually uint

            Encoder.AddArray(message, ref index, (List<byte> buffer, ref int localIndex) =>
            {
                AddHeader(buffer, ref localIndex, 6, destination);
                AddHeader(buffer, ref localIndex, 4, error);
                AddHeader(buffer, ref localIndex, replySerial);
                if (body.Count > 0)
                    AddHeader(buffer, ref localIndex, (Signature)"s");
            });
            Encoder.EnsureAlignment(message, ref index, 8);
            message.AddRange(body);

            var messageArray = message.ToArray();
            await stream.WriteAsync(messageArray, 0, messageArray.Length).ConfigureAwait(false);
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
                var bodyLength = Decoder.GetInt32(fixedLengthHeader, ref index); // Actually uint
                var receivedSerial = Decoder.GetUInt32(fixedLengthHeader, ref index);
                var receivedArrayLength = Decoder.GetInt32(fixedLengthHeader, ref index); // Actually uint
                Alignment.Advance(ref receivedArrayLength, 8);
                var headerBytes = new byte[receivedArrayLength];
                await stream.ReadAsync(headerBytes, 0, headerBytes.Length, token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var header = new MessageHeader(headerBytes);

                var body = new byte[bodyLength];
                if (bodyLength > 0)
                {
                    await stream.ReadAsync(body, 0, body.Length, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                }

                switch (messageType)
                {
                    case 1:
                        handleMethodCall(receivedSerial, header, body);
                        break;
                    case 2:
                        handleMethodReturn(header, body);
                        break;
                    case 3:
                        handleError(header, body);
                        break;
                    case 4:
                        handleSignal(header, body);
                        break;
                }
            }
        }

        private void handleMethodCall(uint replySerial, MessageHeader header, byte[] body)
        {
            var dictionaryEntry = header.Path + "\0" + header.InterfaceName;
            Func<uint, MessageHeader, byte[], Task> proxy;
            if (objectProxies.TryGetValue(dictionaryEntry, out proxy))
                Task.Run(async () =>
                {
                    try
                    {
                        await proxy(replySerial, header, body);
                    }
                    catch (DbusException dbusException)
                    {
                        await sendMethodCallErrorAsync(
                            replySerial,
                            header.Sender,
                            dbusException.ErrorName,
                            dbusException.ErrorMessage
                         );
                    }
                    catch (Exception e)
                    {
                        await sendMethodCallErrorAsync(
                            replySerial,
                            header.Sender,
                            DbusException.CreateErrorName("General"),
                            e.Message
                         );
                    }
                });
            else
                Task.Run(() => sendMethodCallErrorAsync(
                    replySerial,
                    header.Sender,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                ));
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

        private void handleError(MessageHeader header, byte[] body)
        {
            if (header.ReplySerial == 0)
                throw new InvalidOperationException("Only errors for method calls are supported");
            if (!header.BodySignature.ToString().StartsWith("s"))
                throw new InvalidOperationException("Errors are expected to start their body with a string");

            TaskCompletionSource<ReceivedMethodReturn> tcs;
            if (!expectedMessages.TryRemove(header.ReplySerial, out tcs))
                throw new InvalidOperationException("Couldn't find the method call for the error");

            var index = 0;
            var message = Decoder.GetString(body, ref index);
            var exception = new DbusException(header.ErrorName, message);
            tcs.SetException(exception);
        }

        private void handleSignal(
            MessageHeader header,
            byte[] body
        )
        {
            var dictionaryEntry = header.Path + "\0" + header.InterfaceName + "\0" + header.Member;
            Action<MessageHeader, byte[]> handler;
            if (signalHandlers.TryGetValue(dictionaryEntry, out handler))
                Task.Run(() => handler(header, body));
        }

        public void Dispose()
        {
            receiveCts.Cancel();
            stream.Dispose();
        }

        private class deregistration : IDisposable
        {
            public Action Deregister;

            public void Dispose()
            {
                Deregister();
            }
        }
    }
}
