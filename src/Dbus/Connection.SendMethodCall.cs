using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus;

public partial class Connection
{
    private readonly ConcurrentDictionary<uint, TaskCompletionSource<Decoder>> expectedMessages = new();

    private static Encoder buildMethodCallHeader(
        int bodyLength,
        uint serial,
        ObjectPath path,
        string interfaceName,
        string methodName,
        string destination,
        Signature signature
    )
    {
        var encoder = new Encoder();

        standardHeaders(
            encoder,
            DbusMessageType.MethodCall,
            DbusMessageFlags.None,
            bodyLength,
            serial
        );

        var state = encoder.StartArray(storesCompoundValues: false);
        addHeader(encoder, path);
        addHeader(encoder, DbusHeaderType.InterfaceName, interfaceName);
        addHeader(encoder, DbusHeaderType.Member, methodName);
        addHeader(encoder, DbusHeaderType.Destination, destination);
        if (bodyLength > 0)
            addHeader(encoder, signature);
        encoder.FinishArray(state);

        encoder.FinishHeader();
        return encoder;
    }

    public async Task<Decoder> SendMethodCall(
        ObjectPath path,
        string interfaceName,
        string methodName,
        string destination,
        Encoder? body,
        Signature signature,
        CancellationToken cancellationToken
    )
    {
        var serial = getSerial();
        var bodyLength = 0;
        var bodySegments = default(ReadOnlySequence<byte>);

        if (body != null)
        {
            bodySegments = await body.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);
            foreach (var segment in bodySegments)
                bodyLength += segment.Length;
        }

        var header = buildMethodCallHeader(
            bodyLength,
            serial,
            path,
            interfaceName,
            methodName,
            destination,
            signature
        );

        var headerSegments = await header.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

        var tcs = new TaskCompletionSource<Decoder>(TaskCreationOptions.RunContinuationsAsynchronously);
        expectedMessages[serial] = tcs;

        using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            await serializedWriteToStream(
                headerSegments,
                bodySegments,
                cancellationToken
            ).ConfigureAwait(false);

            if (body != null)
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

            tcs.TrySetResult(decoder);
        }
        catch (Exception e)
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

                DbusException exception;
                if (header.BodySignature is null || header.ErrorName is null)
                    exception = new DbusException("Invalid message");
                else if (header.BodySignature.ToString().StartsWith("s"))
                {
                    var message = Decoder.GetString(decoder);
                    exception = new DbusException(header.ErrorName, message);
                }
                else
                    exception = new DbusException(header.ErrorName);

                if (!expectedMessages.TryRemove(header.ReplySerial, out var tcs))
                    throw new InvalidOperationException("Couldn't find the method call for the error", exception);

                tcs.TrySetException(exception);
            }
        }
        catch (Exception e)
        {
            onUnobservedException(e);
        }
    }
}
