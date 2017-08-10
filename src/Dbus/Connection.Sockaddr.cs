using System;
using System.Net.Sockets;
using System.Text;

namespace Dbus
{
    public partial class Connection
    {
        private static byte[] createSockaddr(string address)
        {
            const string unix = "unix:";
            const string pathKey = "path=";
            const string abstractKey = "abstract=";

            if (!address.StartsWith(unix))
                throw new InvalidOperationException("Only unix sockets are supported");

            var startIndex = 0;

            address = address.Substring(unix.Length);
            if (address.StartsWith(pathKey))
            {
                startIndex = 2;
                address = address.Substring(pathKey.Length);
            }
            else if (address.StartsWith(abstractKey))
            {
                startIndex = 3;
                address = address.Substring(abstractKey.Length);
            }
            else
                throw new InvalidOperationException("Unsupported address format");

            var pathBytes = Encoding.ASCII.GetBytes(address);
            var result = new byte[startIndex + pathBytes.Length];

            result[0] = (byte)AddressFamily.Unix;
            for (var i = 0; i < pathBytes.Length; ++i)
                result[startIndex + i] = pathBytes[i];

            return result;
        }
    }
}
