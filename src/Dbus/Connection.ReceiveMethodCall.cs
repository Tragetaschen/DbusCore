using System;
using System.Collections.Concurrent;
using System.Threading;
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
            return new proxyHandle(this, dictionaryEntry);
        }

        private class proxyHandle : IDisposable
        {
            private readonly Connection connection;
            private readonly string entry;

            public proxyHandle(Connection connection, string entry)
            {
                this.connection = connection;
                this.entry = entry;
            }

            public void Dispose()
                => connection.objectProxies.TryRemove(entry, out var _);
        }

        public async Task SendMethodReturnAsync(
            MethodCallOptions methodCallOptions,
            Encoder body,
            Signature signature,
            CancellationToken cancellationToken
        )
        {
            var bodySegments = await body.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);
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

            var headerSegments = await header.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

            await serializedWriteToStream(
                headerSegments,
                bodySegments,
                cancellationToken
            ).ConfigureAwait(false);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);
        }

        private void handleMethodCall(
            MethodCallOptions methodCallOptions,
            ReceivedMessage receivedMessage,
            CancellationToken cancellationToken
        )
        {
            async Task withExceptionHandling(Func<MethodCallOptions, ReceivedMessage, CancellationToken, Task> work, CancellationToken localCancellationToken)
            {
                try
                {
                    using (receivedMessage)
                        await work(methodCallOptions, receivedMessage, localCancellationToken);
                }
                catch (DbusException dbusException)
                {
                    await sendMethodCallErrorAsync(
                        methodCallOptions,
                        dbusException.ErrorName,
                        dbusException.ErrorMessage,
                        localCancellationToken
                    );
                }
                catch (Exception e)
                {
                    await sendMethodCallErrorAsync(
                        methodCallOptions,
                        DbusException.CreateErrorName("General"),
                        e.Message,
                        localCancellationToken
                    );
                }
            }


            if (methodCallOptions.InterfaceName == "org.freedesktop.DBus.Properties")
            {
                Task.Run(() => withExceptionHandling(handlePropertyRequestAsync, cancellationToken));
                return;
            }

            var dictionaryEntry = methodCallOptions.Path + "\0" + methodCallOptions.InterfaceName;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                Task.Run(() => withExceptionHandling(proxy.HandleMethodCallAsync, cancellationToken));
                return;
            }

            receivedMessage.Dispose();
            Task.Run(() => sendMethodCallErrorAsync(
                methodCallOptions,
                DbusException.CreateErrorName("MethodCallTargetNotFound"),
                "The requested method call isn't mapped to an actual object",
                cancellationToken
            ));
        }

        private Task handlePropertyRequestAsync(
            MethodCallOptions methodCallOptions,
            ReceivedMessage receivedMessage,
            CancellationToken cancellationToken
        ) => methodCallOptions.Member switch
        {
            "GetAll" => handleGetAllAsync(methodCallOptions, receivedMessage, cancellationToken),
            "Get" => handleGetAsync(methodCallOptions, receivedMessage, cancellationToken),
            _ => throw new DbusException(
                DbusException.CreateErrorName("UnknownMethod"),
                "Method not supported"
            ),
        };

        private Task handleGetAllAsync(
            MethodCallOptions methodCallOptions,
            ReceivedMessage receivedMessage,
            CancellationToken cancellationToken
        )
        {
            receivedMessage.AssertSignature("s");
            var requestedInterfaces = Decoder.GetString(receivedMessage.Decoder);
            var dictionaryEntry = methodCallOptions.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                var sendBody = new Encoder();
                proxy.EncodeProperties(sendBody);
                return SendMethodReturnAsync(
                    methodCallOptions,
                    sendBody,
                    "a{sv}",
                    cancellationToken
                );
            }
            else
                return sendMethodCallErrorAsync(
                    methodCallOptions,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object",
                    cancellationToken
                );
        }

        private Task handleGetAsync(
            MethodCallOptions methodCallOptions,
            ReceivedMessage receivedMessage,
            CancellationToken cancellationToken
        )
        {
            receivedMessage.AssertSignature("ss");
            var decoder = receivedMessage.Decoder;
            var requestedInterfaces = Decoder.GetString(decoder);
            var requestedProperty = Decoder.GetString(decoder);
            var dictionaryEntry = methodCallOptions.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {
                var sendBody = new Encoder();
                proxy.EncodeProperty(sendBody, requestedProperty);
                return SendMethodReturnAsync(
                    methodCallOptions,
                    sendBody,
                    "v",
                    cancellationToken
                );
            }
            else
                return sendMethodCallErrorAsync(
                    methodCallOptions,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object",
                    cancellationToken
                );
        }

        private async Task sendMethodCallErrorAsync(
            MethodCallOptions methodCallOptions,
            string error,
            string? errorMessage,
            CancellationToken cancellationToken
        )
        {
            var body = new Encoder();
            if (errorMessage != null)
                body.Add(errorMessage);
            var bodySegments = await body.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);
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

            var headerSegments = await header.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

            await serializedWriteToStream(headerSegments, bodySegments, cancellationToken).ConfigureAwait(false);

            body.CompleteReading(bodySegments);
            header.CompleteReading(headerSegments);
        }
    }
}
