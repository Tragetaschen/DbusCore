#if FULL_FRAMEWORK
/*
 cf. ("copied from" ;-))
 https://github.com/dotnet/corefx/blob/release/1.1.0/src/System.Net.Sockets/src/System/Net/Sockets/SocketTaskExtensions.cs#L31
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dbus
{
    internal static class SocketTaskExtensions
    {
        public static Task ConnectAsync(this Socket socket, EndPoint remoteEndPoint)
            => Task.Factory.FromAsync(
                (targetEndPoint, callback, state) => ((Socket)state).BeginConnect(targetEndPoint, callback, state),
                asyncResult => ((Socket)asyncResult.AsyncState).EndConnect(asyncResult),
                remoteEndPoint,
                state: socket);
    }
}
#endif
