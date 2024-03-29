﻿using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus;

internal sealed class OrgFreedesktopDbus(Connection connection) : IOrgFreedesktopDbus
{
    private static readonly ObjectPath path = "/org/freedesktop/DBus";
    private const string interfaceName = "org.freedesktop.DBus";
    private const string destination = "org.freedesktop.DBus";

    public async Task HelloAsync(CancellationToken cancellationToken)
    {
        var receivedMessage = await connection.SendMethodCall(
            path,
            interfaceName,
            "Hello",
            destination,
            new Encoder(),
            "",
            cancellationToken
        ).ConfigureAwait(false);

        using (receivedMessage)
            receivedMessage.AssertSignature("s"); // The own bus name is not required
    }

    public async Task AddMatchAsync(string match, CancellationToken cancellationToken)
    {
        var sendBody = new Encoder();
        sendBody.Add(match);

        var receivedMessage = await connection.SendMethodCall(
            path,
            interfaceName,
            "AddMatch",
            destination,
            sendBody,
            "s",
            cancellationToken
        ).ConfigureAwait(false);

        using (receivedMessage)
            receivedMessage.AssertSignature("");
    }

    public async Task RemoveMatchAsync(string match, CancellationToken cancellationToken)
    {
        var sendBody = new Encoder();
        sendBody.Add(match);

        var receivedMessage = await connection.SendMethodCall(
            path,
            interfaceName,
            "RemoveMatch",
            destination,
            sendBody,
            "s",
            cancellationToken
        ).ConfigureAwait(false);

        using (receivedMessage)
        {
            receivedMessage.AssertSignature("");
            return;
        }
    }

    public async Task<List<string>> ListNamesAsync(CancellationToken cancellationToken)
    {
        var receivedMessage = await connection.SendMethodCall(
            path,
            interfaceName,
            "ListNames",
            destination,
            new Encoder(),
            "",
            cancellationToken
        ).ConfigureAwait(false);

        using (receivedMessage)
        {
            receivedMessage.AssertSignature("as");
            return Decoder.GetArray(receivedMessage, Decoder.GetString, false);
        }
    }

    public ValueTask DisposeAsync() => default;
    public void Dispose() { }
}
