using System;
using System.Text;

namespace Dbus
{
    public partial class Connection
    {
        private const string newline = "\r\n";

        private static void authenticate(int socketHandle)
        {
            var authExternal = "\0AUTH EXTERNAL ";

            var uid = UnsafeNativeMethods.getuid();
            var stringUid = $"{uid}";
            var uidBytes = Encoding.ASCII.GetBytes(stringUid);
            foreach (var b in uidBytes)
                authExternal += $"{b:X}";
            writeLine(socketHandle, authExternal);

            var line = readLine(socketHandle);
            if (!line.StartsWith("OK "))
                throw new InvalidOperationException("Authentication failed: " + line);

            writeLine(socketHandle, "NEGOTIATE_UNIX_FD");

            line = readLine(socketHandle);
            if (line != "AGREE_UNIX_FD")
                throw new InvalidOperationException("Missing support for unix file descriptors");

            writeLine(socketHandle, "BEGIN");
        }


        private static void writeLine(int socketHandle, string contents)
        {
            contents += newline;
            var sendBytes = Encoding.ASCII.GetBytes(contents);
            var sendResult = UnsafeNativeMethods.send(socketHandle, sendBytes, sendBytes.Length, 0);
            if (sendResult <= 0)
                throw new InvalidOperationException("Could not send");
        }

        private static string readLine(int socketHandle)
        {
            var line = "";
            var receiveByte = new byte[1];
            while (!line.EndsWith(newline))
            {
                var result = UnsafeNativeMethods.recv(socketHandle, receiveByte, 1, 0);
                if (result != 1)
                    throw new InvalidOperationException("recv failed: " + result);
                line += Encoding.ASCII.GetString(receiveByte);
            }

            var toReturn = line.Substring(0, line.Length - newline.Length);
            return toReturn;
        }
    }
}
