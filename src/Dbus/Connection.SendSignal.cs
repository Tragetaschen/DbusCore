using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus;

public partial class Connection
{
    private Encoder buildSignalHeader(
        int bodyLength,
        ObjectPath path,
        string interfaceName,
        string methodName,
        Signature signature
    )
    {
        var encoder = new Encoder();

        standardHeaders(
            encoder,
            DbusMessageType.Signal,
            DbusMessageFlags.None,
            bodyLength,
            getSerial()
        );

        var state = encoder.StartArray(storesCompoundValues: false);
        addHeader(encoder, path);
        addHeader(encoder, DbusHeaderType.InterfaceName, interfaceName);
        addHeader(encoder, DbusHeaderType.Member, methodName);
        if (bodyLength > 0)
            addHeader(encoder, signature);
        encoder.FinishArray(state);

        encoder.FinishHeader();
        return encoder;
    }

    public async Task SendSignalAsync(
        ObjectPath path,
        string interfaceName,
        string methodName,
        Encoder body,
        Signature signature,
        CancellationToken cancellationToken
    )
    {
        var bodySegments = await body.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

        if (path.ToString() == "")
            throw new ArgumentException("Signal path must not be empty", nameof(path));
        if (interfaceName == "")
            throw new ArgumentException("Signal interface must not be empty", nameof(interfaceName));
        if (methodName == "")
            throw new ArgumentException("Signal member must not be empty", nameof(methodName));

        var bodyLength = 0;
        foreach (var segment in bodySegments)
            bodyLength += segment.Length;

        var header = buildSignalHeader(
            bodyLength,
            path,
            interfaceName,
            methodName,
            signature
        );

        var headerSegments = await header.CompleteWritingAsync(cancellationToken).ConfigureAwait(false);

        await serializedWriteToStream(headerSegments, bodySegments, cancellationToken);

        body.CompleteReading(bodySegments);
        header.CompleteReading(headerSegments);
    }
}
