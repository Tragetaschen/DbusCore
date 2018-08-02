using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        private readonly ConcurrentDictionary<string, IProxy> objectProxies =
            new ConcurrentDictionary<string, IProxy>();

        public IDisposable RegisterObjectProxy(
            ObjectPath path,
            string interfaceName,
            IProxy proxy
        )
        {
            var dictionaryEntry = path + "\0" + interfaceName;
            if (!objectProxies.TryAdd(dictionaryEntry, proxy))
                throw new InvalidOperationException("Attempted to register an object proxy twice");

            return deregisterVia(deregister);

            void deregister()
            {
                objectProxies.TryRemove(dictionaryEntry, out var _);
            }
        }

        public async Task SendMethodReturnAsync(uint replySerial, string destination, Encoder body, Signature signature)
        {
            var bodySegments = await body.FinishAsync().ConfigureAwait(false);
            var bodyLength = 0;
            foreach (var bodySegment in bodySegments)
                bodyLength += bodySegment.Length;

            var header = new Encoder();
            header.Add((byte)dbusEndianess.LittleEndian);
            header.Add((byte)dbusMessageType.MethodReturn);
            header.Add((byte)dbusFlags.NoReplyExpected);
            header.Add((byte)dbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(getSerial());

            header.AddArray(() =>
            {
                addHeader(header, 6, destination);
                addHeader(header, replySerial);
                if (bodyLength > 0)
                    addHeader(header, signature);
            });
            header.EnsureAlignment(8);

            var headerSegments = await header.FinishAsync().ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments).ConfigureAwait(false);
        }

        private void handleMethodCall(
                uint replySerial,
                MessageHeader header,
                IMemoryOwner<byte> body,
                int bodyLength,
                bool shouldSendReply
            )
        {
            if (header.InterfaceName == "org.freedesktop.DBus.Properties")
            {
                Task.Run(() =>
                {
                    try
                    {
                        var decoder = new Decoder(body.Memory, bodyLength);
                        handlePropertyRequestAsync(replySerial, header, decoder);
                    }
                    finally
                    {
                        body.Dispose();
                    }
                });
                return;
            }
            var dictionaryEntry = header.Path + "\0" + header.InterfaceName;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
                Task.Run(() =>
                {
                    try
                    {
                        var decoder = new Decoder(body.Memory, bodyLength);
                        return proxy.HandleMethodCallAsync(replySerial, header, decoder, shouldSendReply);
                    }
                    catch (DbusException dbusException)
                    {
                        return sendMethodCallErrorAsync(
                            replySerial,
                            header.Sender,
                            dbusException.ErrorName,
                            dbusException.ErrorMessage
                         );
                    }
                    catch (Exception e)
                    {
                        return sendMethodCallErrorAsync(
                            replySerial,
                            header.Sender,
                            DbusException.CreateErrorName("General"),
                            e.Message
                         );
                    }
                    finally
                    {
                        body.Dispose();
                    }
                });
            else
            {
                Task.Run(() => sendMethodCallErrorAsync(
                    replySerial,
                    header.Sender,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                ));
                body.Dispose();
            }
        }

        private Task handlePropertyRequestAsync(uint replySerial, MessageHeader header, Decoder body)
        {
            switch (header.Member)
            {
                case "GetAll":
                    return handleGetAllAsync(replySerial, header, body);
                case "Get":
                    return handleGetAsync(replySerial, header, body);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }
        private Task handleGetAllAsync(uint replySerial, MessageHeader header, Decoder body)
        {
            header.BodySignature.AssertEqual("s");
            var requestedInterfaces = body.GetString();
            var dictionaryEntry = header.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                var sendBody = new Encoder();
                proxy.EncodeProperties(sendBody);
                return SendMethodReturnAsync(replySerial, header.Sender, sendBody, "a{sv}");
            }
            else
                return sendMethodCallErrorAsync(
                    replySerial,
                    header.Sender,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                );
        }

        private Task handleGetAsync(uint replySerial, MessageHeader header, Decoder body)
        {
            header.BodySignature.AssertEqual("ss");
            var requestedInterfaces = body.GetString();
            var requestedProperty = body.GetString();
            var dictionaryEntry = header.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                var sendBody = new Encoder();
                proxy.EncodeProperty(sendBody, requestedProperty);
                return SendMethodReturnAsync(replySerial, header.Sender, sendBody, "v");
            }
            else
                return sendMethodCallErrorAsync(
                    replySerial,
                    header.Sender,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                );
        }

        private async Task sendMethodCallErrorAsync(uint replySerial, string destination, string error, string errorMessage)
        {
            var body = new Encoder();
            body.Add(errorMessage);
            var bodySegments = await body.FinishAsync().ConfigureAwait(false);
            var bodyLength = 0;
            foreach (var segment in bodySegments)
                bodyLength += segment.Length;

            var header = new Encoder();
            header.Add((byte)dbusEndianess.LittleEndian);
            header.Add((byte)dbusMessageType.Error);
            header.Add((byte)dbusFlags.NoReplyExpected);
            header.Add((byte)dbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(getSerial());

            header.AddArray(() =>
            {
                addHeader(header, 6, destination);
                addHeader(header, 4, error);
                addHeader(header, replySerial);
                if (bodyLength > 0)
                    addHeader(header, (Signature)"s");
            });
            header.EnsureAlignment(8);

            var headerSegments = await header.FinishAsync().ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments).ConfigureAwait(false);
        }
    }
}
