using System;
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

        public async Task SendMethodReturnAsync(MethodCallOptions methodCallOptions, Encoder body, Signature signature)
        {
            var bodySegments = await body.CompleteWritingAsync().ConfigureAwait(false);
            var bodyLength = 0;
            foreach (var bodySegment in bodySegments)
                bodyLength += bodySegment.Length;

            var header = new Encoder();
            header.Add((byte)DbusEndianess.LittleEndian);
            header.Add((byte)DbusMessageType.MethodReturn);
            header.Add((byte)DbusMessageFlags.NoReplyExpected);
            header.Add((byte)DbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(getSerial());

            header.AddArray(() =>
            {
                addHeader(header, 6, methodCallOptions.Sender);
                addHeader(header, methodCallOptions.ReplySerial);
                if (bodyLength > 0)
                    addHeader(header, signature);
            });
            header.EnsureAlignment(8);

            var headerSegments = await header.CompleteWritingAsync().ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments).ConfigureAwait(false);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);
        }

        private void handleMethodCall(
            MethodCallOptions methodCallOptions,
            ReceivedMessage receivedMessage
        )
        {
            if (methodCallOptions.InterfaceName == "org.freedesktop.DBus.Properties")
            {
                Task.Run(() =>
                {
                    using (receivedMessage)
                        handlePropertyRequestAsync(methodCallOptions, receivedMessage);
                });
                return;
            }
            var dictionaryEntry = methodCallOptions.Path + "\0" + methodCallOptions.InterfaceName;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
                Task.Run(() =>
                {
                    try
                    {
                        using (receivedMessage)
                            return proxy.HandleMethodCallAsync(methodCallOptions, receivedMessage);
                    }
                    catch (DbusException dbusException)
                    {
                        return sendMethodCallErrorAsync(
                            methodCallOptions,
                            dbusException.ErrorName,
                            dbusException.ErrorMessage
                         );
                    }
                    catch (Exception e)
                    {
                        return sendMethodCallErrorAsync(
                            methodCallOptions,
                            DbusException.CreateErrorName("General"),
                            e.Message
                         );
                    }
                });
            else
            {
                receivedMessage.Dispose();
                Task.Run(() => sendMethodCallErrorAsync(
                    methodCallOptions,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                ));
            }
        }

        private Task handlePropertyRequestAsync(MethodCallOptions methodCallOptions, ReceivedMessage receivedMessage)
        {
            switch (methodCallOptions.Member)
            {
                case "GetAll":
                    return handleGetAllAsync(methodCallOptions, receivedMessage);
                case "Get":
                    return handleGetAsync(methodCallOptions, receivedMessage);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }
        private Task handleGetAllAsync(MethodCallOptions methodCallOptions, ReceivedMessage receivedMessage)
        {
            receivedMessage.AssertSignature("s");
            var requestedInterfaces = receivedMessage.Decoder.GetString();
            var dictionaryEntry = methodCallOptions.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                var sendBody = new Encoder();
                proxy.EncodeProperties(sendBody);
                return SendMethodReturnAsync(methodCallOptions, sendBody, "a{sv}");
            }
            else
                return sendMethodCallErrorAsync(
                    methodCallOptions,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                );
        }

        private Task handleGetAsync(MethodCallOptions methodCallOptions, ReceivedMessage receivedMessage)
        {
            receivedMessage.AssertSignature("ss");
            var decoder = receivedMessage.Decoder;
            var requestedInterfaces = decoder.GetString();
            var requestedProperty = decoder.GetString();
            var dictionaryEntry = methodCallOptions.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                var sendBody = new Encoder();
                proxy.EncodeProperty(sendBody, requestedProperty);
                return SendMethodReturnAsync(methodCallOptions, sendBody, "v");
            }
            else
                return sendMethodCallErrorAsync(
                    methodCallOptions,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                );
        }

        private async Task sendMethodCallErrorAsync(MethodCallOptions methodCallOptions, string error, string errorMessage)
        {
            var body = new Encoder();
            body.Add(errorMessage);
            var bodySegments = await body.CompleteWritingAsync().ConfigureAwait(false);
            var bodyLength = 0;
            foreach (var segment in bodySegments)
                bodyLength += segment.Length;

            var header = new Encoder();
            header.Add((byte)DbusEndianess.LittleEndian);
            header.Add((byte)DbusMessageType.Error);
            header.Add((byte)DbusMessageFlags.NoReplyExpected);
            header.Add((byte)DbusProtocolVersion.Default);
            header.Add(bodyLength); // Actually uint
            header.Add(getSerial());

            header.AddArray(() =>
            {
                addHeader(header, 6, methodCallOptions.Sender);
                addHeader(header, 4, error);
                addHeader(header, methodCallOptions.ReplySerial);
                if (bodyLength > 0)
                    addHeader(header, (Signature)"s");
            });
            header.EnsureAlignment(8);

            var headerSegments = await header.CompleteWritingAsync().ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments).ConfigureAwait(false);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);
        }
    }
}
