﻿using System;
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

            var header = createHeader(
                DbusMessageType.MethodReturn,
                DbusMessageFlags.NoReplyExpected,
                bodyLength,
                e =>
                {
                    addHeader(e, DbusHeaderType.Destination, methodCallOptions.Sender);
                    addHeader(e, methodCallOptions.ReplySerial);
                    if (bodyLength > 0)
                        addHeader(e, signature);
                }
            );

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
            async Task withExceptionHandling(Func<MethodCallOptions, ReceivedMessage, Task> work)
            {
                try
                {
                    using (receivedMessage)
                        await work(methodCallOptions, receivedMessage);
                }
                catch (DbusException dbusException)
                {
                    await sendMethodCallErrorAsync(
                        methodCallOptions,
                        dbusException.ErrorName,
                        dbusException.ErrorMessage
                    );
                }
                catch (Exception e)
                {
                    await sendMethodCallErrorAsync(
                        methodCallOptions,
                        DbusException.CreateErrorName("General"),
                        e.Message
                    );
                }
            }


            if (methodCallOptions.InterfaceName == "org.freedesktop.DBus.Properties")
            {
                Task.Run(() => withExceptionHandling(handlePropertyRequestAsync));
                return;
            }

            var dictionaryEntry = methodCallOptions.Path + "\0" + methodCallOptions.InterfaceName;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                Task.Run(() => withExceptionHandling(proxy.HandleMethodCallAsync));
                return;
            }

            receivedMessage.Dispose();
            Task.Run(() => sendMethodCallErrorAsync(
                methodCallOptions,
                DbusException.CreateErrorName("MethodCallTargetNotFound"),
                "The requested method call isn't mapped to an actual object"
            ));
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

            var header = createHeader(
                DbusMessageType.Error,
                DbusMessageFlags.NoReplyExpected,
                bodyLength,
                e =>
                {
                    addHeader(e, DbusHeaderType.Destination, methodCallOptions.Sender);
                    addHeader(e, DbusHeaderType.ErrorName, error);
                    addHeader(e, methodCallOptions.ReplySerial);
                    if (bodyLength > 0)
                        addHeader(e, (Signature)"s");
                }
            );

            var headerSegments = await header.CompleteWritingAsync().ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments).ConfigureAwait(false);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);
        }
    }
}
