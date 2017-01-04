using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Dbus
{
    public class OrgFreedesktopDbus : IDisposable
    {
        private readonly Connection connection;
        private readonly List<IDisposable> eventSubscriptions = new List<IDisposable>();

        public OrgFreedesktopDbus(Connection connection)
        {
            this.connection = connection;

            var deregistration = connection.RegisterSignalHandler(
                "/org/freedesktop/DBus",
                "org.freedesktop.DBus",
                "NameAcquired",
                handleNameAcquired
            );
            eventSubscriptions.Add(deregistration);
        }

        public async Task<string> HelloAsync()
        {
            var receivedMessage = await connection.SendMethodCall(
                "/org/freedesktop/DBus",
                "org.freedesktop.DBus",
                "Hello",
                "org.freedesktop.DBus"
            );
            if (receivedMessage.Signature != "s")
                throw new InvalidOperationException("Unexpected body signature");

            var body = receivedMessage.Body;
            var stringLength = BitConverter.ToInt32(body, 0);
            var path = Encoding.UTF8.GetString(body, 4, stringLength);

            return path;
        }

        public event Action<string> NameAcquired;
        private void handleNameAcquired(MessageHeader header, byte[] body)
        {
            if (header.BodySignature != "s")
                throw new InvalidOperationException("Unexpected body signature");

            var stringLength = BitConverter.ToInt32(body, 0);
            var name = Encoding.UTF8.GetString(body, 4, stringLength);

            NameAcquired?.Invoke(name);
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }
}
