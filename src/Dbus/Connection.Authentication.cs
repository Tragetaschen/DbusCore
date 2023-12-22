using System;
using System.Globalization;
using System.Text;

namespace Dbus;

public partial class Connection
{
    private static void authenticate(SocketOperations socketOperations)
    {
        var stringUid = socketOperations.Uid.ToString(CultureInfo.InvariantCulture);
        var uidBytes = Encoding.ASCII.GetBytes(stringUid);

        var authExternal = new StringBuilder("\0AUTH EXTERNAL ");
        foreach (var b in uidBytes)
            authExternal.Append(b.ToString("X", CultureInfo.InvariantCulture));
        socketOperations.WriteLine(authExternal.ToString());

        var line = socketOperations.ReadLine();
        if (!line.StartsWith("OK "))
            throw new InvalidOperationException("Authentication failed: " + line);

        socketOperations.WriteLine("NEGOTIATE_UNIX_FD");

        line = socketOperations.ReadLine();
        if (line != "AGREE_UNIX_FD")
            throw new InvalidOperationException("Missing support for unix file descriptors");

        socketOperations.WriteLine("BEGIN");
    }
}
