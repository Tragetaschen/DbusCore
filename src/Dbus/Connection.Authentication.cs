using System;
using System.Text;

namespace Dbus
{
    public partial class Connection
    {
        private static void authenticate(ISocketOperations socketOperations)
        {
            var authExternal = "\0AUTH EXTERNAL ";

            var stringUid = $"{socketOperations.Uid}";
            var uidBytes = Encoding.ASCII.GetBytes(stringUid);
            foreach (var b in uidBytes)
                authExternal += $"{b:X}";
            socketOperations.WriteLine(authExternal);

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
}
