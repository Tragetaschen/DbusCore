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
            assertSignature(receivedMessage.Signature, "s");

            var body = receivedMessage.Body;
            var stringLength = BitConverter.ToInt32(body, 0);
            var path = Encoding.UTF8.GetString(body, 4, stringLength);

            return path;
        }

        public async Task<IEnumerable<string>> ListNamesAsync()
        {
            var receivedMessage = await connection.SendMethodCall(
                "/org/freedesktop/DBus",
                "org.freedesktop.DBus",
                "ListNames",
                "org.freedesktop.DBus"
            );
            assertSignature(receivedMessage.Signature, "as");
            var body = receivedMessage.Body;
            var index = 0;
            var arrayLength = BitConverter.ToInt32(body, index);
            index += 4;
            var names = new List<string>();
            while (index < arrayLength)
            {
                index += Alignment.Calculate(index, 4);
                var stringLength = BitConverter.ToInt32(body, index);
                index += 4;
                var name = Encoding.UTF8.GetString(body, index, stringLength);
                Console.WriteLine($"{stringLength} {name} {index}");
                names.Add(name);
                index += stringLength + 1;
            }
            return names;
        }

        public event Action<string> NameAcquired;
        private void handleNameAcquired(MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, "s");

            var stringLength = BitConverter.ToInt32(body, 0);
            var name = Encoding.UTF8.GetString(body, 4, stringLength);

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
