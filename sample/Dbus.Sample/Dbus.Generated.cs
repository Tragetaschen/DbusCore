namespace Dbus.Sample
{

    public sealed class OrgFreedesktopUpower : Dbus.Sample.IOrgFreedesktopUpower
    {
        private readonly Connection connection;
        private readonly ObjectPath path;
        private readonly string destination;
        private readonly System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new System.Collections.Generic.List<System.IDisposable>();

        public OrgFreedesktopUpower(Connection connection, ObjectPath path = null, string destination = "")
        {
            this.connection = connection;
            this.path = path == null ? "/org/freedesktop/UPower" : path;
            this.destination = destination == "" ? "org.freedesktop.UPower" : destination;

        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IDictionary<System.String,System.Object>> GetAllAsync(System.String interfaceName)
        {
            var sendBody = Encoder.StartNew();
            var sendIndex = 0;
            Encoder.Add(sendBody, ref sendIndex, interfaceName);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.UPower",
                "GetAll",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "a{sv}");
            var index = 0;
            var result = Decoder.GetDictionary(receivedMessage.Body, ref index, Decoder.GetString, Decoder.GetObject);
            return result;

        }


        private static void assertSignature(Signature actual, Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($"Unexpected signature. Got ${ actual}, but expected ${ expected}");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }

    public sealed class OrgFreedesktopDbus : Dbus.IOrgFreedesktopDbus
    {
        private readonly Connection connection;
        private readonly ObjectPath path;
        private readonly string destination;
        private readonly System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new System.Collections.Generic.List<System.IDisposable>();

        public OrgFreedesktopDbus(Connection connection, ObjectPath path = null, string destination = "")
        {
            this.connection = connection;
            this.path = path == null ? "/org/freedesktop/DBus" : path;
            this.destination = destination == "" ? "org.freedesktop.DBus" : destination;
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                path,
                "org.freedesktop.DBus",
                "NameAcquired",
                handleNameAcquired
            ));

        }

        public async System.Threading.Tasks.Task<System.String> HelloAsync()
        {
            var sendBody = Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "Hello",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "s");
            var index = 0;
            var result = Decoder.GetString(receivedMessage.Body, ref index);
            return result;

        }

        public async System.Threading.Tasks.Task<System.Collections.Generic.IEnumerable<System.String>> ListNamesAsync()
        {
            var sendBody = Encoder.StartNew();

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "ListNames",
                destination,
                sendBody,
                ""
            );
            assertSignature(receivedMessage.Signature, "as");
            var index = 0;
            var result = Decoder.GetArray(receivedMessage.Body, ref index, Decoder.GetString);
            return result;

        }

        public async System.Threading.Tasks.Task<System.UInt32> RequestNameAsync(System.String name, System.UInt32 flags)
        {
            var sendBody = Encoder.StartNew();
            var sendIndex = 0;
            Encoder.Add(sendBody, ref sendIndex, name);
            Encoder.Add(sendBody, ref sendIndex, flags);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "RequestName",
                destination,
                sendBody,
                "su"
            );
            assertSignature(receivedMessage.Signature, "u");
            var index = 0;
            var result = Decoder.GetUInt32(receivedMessage.Body, ref index);
            return result;

        }

        public event System.Action<System.String> NameAcquired;
        private void handleNameAcquired(MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, "s");
            var index = 0;
            var decoded = Decoder.GetString(body, ref index);
            NameAcquired?.Invoke(decoded);
        }

        private static void assertSignature(Signature actual, Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($"Unexpected signature. Got ${ actual}, but expected ${ expected}");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }

}
