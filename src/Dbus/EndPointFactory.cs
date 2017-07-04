using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Dbus
{
    public static class EndPointFactory
    {
        private const string unix = "unix:";
        private const string pathKey = "path=";
        private const string abstractKey = "abstract=";

        public static EndPoint Create(string address)
        {
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
            var result = new SocketAddress(AddressFamily.Unix, startIndex + pathBytes.Length);
            for (var i = 0; i < pathBytes.Length; ++i)
                result[startIndex + i] = pathBytes[i];

            return new systemBusEndPoint(result);
        }

        private class systemBusEndPoint : EndPoint
        {
            private readonly SocketAddress address;

            public systemBusEndPoint(SocketAddress address) => this.address = address;

            public override SocketAddress Serialize() => address;
        }
    }
}
