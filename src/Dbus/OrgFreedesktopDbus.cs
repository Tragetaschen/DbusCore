using System;
using System.Text;
using System.Threading.Tasks;

namespace Dbus
{
    public class OrgFreedesktopDbus
    {
        private readonly Connection connection;

        public OrgFreedesktopDbus(Connection connection)
        {
            this.connection = connection;
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
    }
}
