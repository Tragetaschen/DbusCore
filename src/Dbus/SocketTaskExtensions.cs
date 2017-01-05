#if FULL_FRAMEWORK
/*
 cf. ("copied from" ;-))
 https://github.com/dotnet/corefx/blob/bffef76f6af208e2042a2f27bc081ee908bb390b/src/System.Net.Sockets/tests/FunctionalTests/ConnectAsync.cs
 */

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Dbus
{
    internal static class SocketTaskExtensions
    {
        public static Task ConnectAsync(this Socket socket, EndPoint remoteEP)
        {
            var tcs = new TaskCompletionSource<bool>(socket);
            socket.BeginConnect(remoteEP, iar =>
            {
                var innerTcs = (TaskCompletionSource<bool>)iar.AsyncState;
                try
                {
                    ((Socket)innerTcs.Task.AsyncState).EndConnect(iar);
                    innerTcs.TrySetResult(true);
                }
                catch (Exception e) { innerTcs.TrySetException(e); }
            }, tcs);
            return tcs.Task;
        }
    }
}
#endif
