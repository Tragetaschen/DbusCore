using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public class OrgFreedesktopUpower : IDisposable
    {
        private readonly Connection connection;

        public OrgFreedesktopUpower(Connection connection)
        {
            this.connection = connection;
        }

        public async Task<IDictionary<string, object>> GetAllAsync()
        {
            var sendBody = Encoder.StartNew();
            var sendIndex = 0;

            Encoder.Add(sendBody, ref sendIndex, "org.freedesktop.UPower");

            var receivedMessage = await connection.SendMethodCall(
                "/org/freedesktop/UPower",
                "org.freedesktop.DBus.Properties",
                "GetAll",
                "org.freedesktop.UPower",
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "a{sv}");

            var body = receivedMessage.Body;
            var index = 0;
            var result = Decoder.GetDictionary(
                body,
                ref index,
                Decoder.GetString,
                Decoder.GetVariant
            );
            return result;
        }

        private static void assertSignature(Signature actual, Signature expected)
        {
            if (actual != expected)
                throw new InvalidOperationException($"Unexpected signature. Got ${actual}, but expected ${expected}");
        }

        public void Dispose()
        {
        }
    }
}
