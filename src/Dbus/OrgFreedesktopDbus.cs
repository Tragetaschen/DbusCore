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
            var sendBody = Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                "/org/freedesktop/DBus",
                "org.freedesktop.DBus",
                "Hello",
                "org.freedesktop.DBus",
                sendBody,
                string.Empty
            );
            assertSignature(receivedMessage.Signature, "s");

            var body = receivedMessage.Body;
            var index = 0;
            var path = Decoder.GetString(body, ref index);
            return path;
        }

        public async Task<IEnumerable<string>> ListNamesAsync()
        {
            var sendBody = Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                "/org/freedesktop/DBus",
                "org.freedesktop.DBus",
                "ListNames",
                "org.freedesktop.DBus",
                sendBody,
                string.Empty
            );
            assertSignature(receivedMessage.Signature, "as");
            var body = receivedMessage.Body;
            var index = 0;
            var names = Decoder.GetArray(body, ref index, Decoder.GetString);
            return names;
        }

        public event Action<string> NameAcquired;
        private void handleNameAcquired(MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, "s");

            var index = 0;
            var name = Decoder.GetString(body, ref index);

            NameAcquired?.Invoke(name);
        }

        private static void assertSignature(string actual, string expected)
        {
            if (actual != expected)
                throw new InvalidOperationException($"Unexpected signature. Got ${actual}, but expected ${expected}");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }
}
