using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus;

public partial class Connection
{
    private readonly ConcurrentDictionary<(ObjectPath path, string interfaceName), IProxy> objectProxies = new();

    public IDisposable RegisterObjectProxy(
        ObjectPath path,
        string interfaceName,
        IProxy proxy
    )
    {
        if (!objectProxies.TryAdd((path, interfaceName), proxy))
            throw new InvalidOperationException("Attempted to register an object proxy twice");
        return new proxyHandle(this, path, interfaceName);
    }

    private class proxyHandle : IDisposable
    {
        private readonly Connection connection;
        private readonly ObjectPath path;
        private readonly string interfaceName;

        public proxyHandle(
            Connection connection,
            ObjectPath path,
            string interfaceName
        )
        {
            this.connection = connection;
            this.path = path;
            this.interfaceName = interfaceName;
        }

        public void Dispose()
            => connection.objectProxies.TryRemove((path, interfaceName), out var _);
    }

    private Encoder buildMethodReturnHeaders(
        int bodyLength,
        MethodCallOptions methodCallOptions,
        Signature signature
    )
    {
        var encoder = new Encoder();
        standardHeaders(
            encoder,
            DbusMessageType.MethodReturn,
            DbusMessageFlags.NoReplyExpected,
            bodyLength,
            getSerial()
        );
        var state = encoder.StartArray(false);
        addHeader(encoder, DbusHeaderType.Destination, methodCallOptions.Sender);
        addHeader(encoder, methodCallOptions.ReplySerial);
        if (bodyLength > 0)
            addHeader(encoder, signature);
        encoder.FinishArray(state);

        encoder.FinishHeader();
        return encoder;
    }

    public async Task SendMethodReturnAsync(
        MethodCallOptions methodCallOptions,
        Encoder? body,
        Signature signature,
        CancellationToken cancellationToken
    )
    {
        var bodyLength = 0;
        var bodySegments = default(ReadOnlySequence<byte>);

        if (body != null)
        {
            bodySegments = await body.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);
            foreach (var bodySegment in bodySegments)
                bodyLength += bodySegment.Length;
        }

        var header = buildMethodReturnHeaders(bodyLength, methodCallOptions, signature);
        var headerSegments = await header.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

        await serializedWriteToStream(
            headerSegments,
            bodySegments,
            cancellationToken
        ).ConfigureAwait(false);

        body?.CompleteReading(bodySegments);
        header.CompleteReading(headerSegments);
    }

    private void handleMethodCall(
        MethodCallOptions methodCallOptions,
        Decoder decoder,
        CancellationToken cancellationToken
    )
    {
        async Task withExceptionHandling(Func<MethodCallOptions, Decoder, CancellationToken, Task> work, CancellationToken localCancellationToken)
        {
            try
            {
                using (decoder)
                    await work(methodCallOptions, decoder, localCancellationToken);
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
            Task.Run(() => withExceptionHandling(handlePropertyRequestAsync, cancellationToken), cancellationToken);
            return;
        }

        var dictionaryEntry = (methodCallOptions.Path, methodCallOptions.InterfaceName);
        if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
        {
            Task.Run(() => withExceptionHandling(proxy.HandleMethodCallAsync, cancellationToken), cancellationToken);
            return;
        }

        decoder.Dispose();
        Task.Run(() => sendMethodCallErrorAsync(
            methodCallOptions,
            DbusException.CreateErrorName("MethodCallTargetNotFound"),
            "The requested method call isn't mapped to an actual object",
            cancellationToken
        ), cancellationToken);
    }

    private Task handlePropertyRequestAsync(
        MethodCallOptions methodCallOptions,
        Decoder decoder,
        CancellationToken cancellationToken
    ) => methodCallOptions.Member switch
    {
        "GetAll" => handleGetAllAsync(methodCallOptions, decoder, cancellationToken),
        "Get" => handleGetAsync(methodCallOptions, decoder, cancellationToken),
        "Set" => handleSetAsync(methodCallOptions, decoder, cancellationToken),
        _ => throw new DbusException(
            DbusException.CreateErrorName("UnknownMethod"),
            "Method not supported"
        ),
    };

    private Task handleGetAllAsync(
        MethodCallOptions methodCallOptions,
        Decoder decoder,
        CancellationToken cancellationToken
    )
    {
        decoder.AssertSignature("s");
        var requestedInterfaces = Decoder.GetString(decoder);
        var dictionaryEntry = (methodCallOptions.Path, requestedInterfaces);
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
        Decoder decoder,
        CancellationToken cancellationToken
    )
    {
        decoder.AssertSignature("ss");
        var requestedInterfaces = Decoder.GetString(decoder);
        var requestedProperty = Decoder.GetString(decoder);
        var dictionaryEntry = (methodCallOptions.Path, requestedInterfaces);
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

    private Task handleSetAsync(
        MethodCallOptions methodCallOptions,
        Decoder decoder,
        CancellationToken cancellationToken
    )
    {
        decoder.AssertSignature("ssv");
        var requestedInterfaces = Decoder.GetString(decoder);
        var requestedProperty = Decoder.GetString(decoder);
        var dictionaryEntry = (methodCallOptions.Path, requestedInterfaces);
        if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
        {
            proxy.SetProperty(requestedProperty, decoder);
            return SendMethodReturnAsync(
                methodCallOptions,
                new Encoder(),
                "",
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

    private Encoder buildMethodCallErrorHeader(
        int bodyLength,
        MethodCallOptions methodCallOptions,
        string error
    )
    {
        var encoder = new Encoder();
        standardHeaders(
            encoder,
            DbusMessageType.Error,
            DbusMessageFlags.NoReplyExpected,
            bodyLength,
            getSerial()
        );

        var state = encoder.StartArray(storesCompoundValues: false);
        addHeader(encoder, DbusHeaderType.Destination, methodCallOptions.Sender);
        addHeader(encoder, DbusHeaderType.ErrorName, error);
        addHeader(encoder, methodCallOptions.ReplySerial);
        if (bodyLength > 0)
            addHeader(encoder, (Signature)"s");
        encoder.FinishArray(state);

        encoder.FinishHeader();
        return encoder;
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

        var header = buildMethodCallErrorHeader(bodyLength, methodCallOptions, error);

        var headerSegments = await header.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

        await serializedWriteToStream(headerSegments, bodySegments, cancellationToken).ConfigureAwait(false);

        body.CompleteReading(bodySegments);
        header.CompleteReading(headerSegments);
    }
}
