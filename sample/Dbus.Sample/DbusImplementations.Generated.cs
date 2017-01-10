namespace Dbus.Sample
{

    public static partial class DbusImplementations
    {
        static partial void DoInit()
        {
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.IOrgFreedesktopDbus>(OrgFreedesktopDbus.Factory);
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.Sample.IOrgFreedesktopUpower>(OrgFreedesktopUpower.Factory);
            global::Dbus.Connection.AddPublishProxy<global::Dbus.Sample.SampleObject>(SampleObject_Proxy.Factory);
        }
    }

    public sealed class OrgFreedesktopDbus : global::Dbus.IOrgFreedesktopDbus
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new global::System.Collections.Generic.List<System.IDisposable>();

        private OrgFreedesktopDbus(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            this.connection = connection;
            this.path = path ?? "/org/freedesktop/DBus";
            this.destination = destination ?? "org.freedesktop.DBus";
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus",
                "NameAcquired",
                handleNameAcquired
            ));

        }

        public static global::Dbus.IOrgFreedesktopDbus Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            return new OrgFreedesktopDbus(connection, path, destination);
        }


        public async global::System.Threading.Tasks.Task AddMatchAsync(global::System.String match)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, match);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "AddMatch",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "");
            return;

        }

        public async global::System.Threading.Tasks.Task<global::System.String> HelloAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

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
            var result = global::Dbus.Decoder.GetString(receivedMessage.Body, ref index);
            return result;

        }

        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IEnumerable<global::System.String>> ListNamesAsync()
        {
            var sendBody = global::Dbus.Encoder.StartNew();

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
            var result = global::Dbus.Decoder.GetArray(receivedMessage.Body, ref index, global::Dbus.Decoder.GetString);
            return result;

        }

        public async global::System.Threading.Tasks.Task RemoveMatchAsync(global::System.String match)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, match);

            var receivedMessage = await connection.SendMethodCall(
                path,
                "org.freedesktop.DBus",
                "RemoveMatch",
                destination,
                sendBody,
                "s"
            );
            assertSignature(receivedMessage.Signature, "");
            return;

        }

        public async global::System.Threading.Tasks.Task<global::System.UInt32> RequestNameAsync(global::System.String name, global::System.UInt32 flags)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, name);
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, flags);

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
            var result = global::Dbus.Decoder.GetUInt32(receivedMessage.Body, ref index);
            return result;

        }

        public event global::System.Action<global::System.String> NameAcquired;
        private void handleNameAcquired(global::Dbus.MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, "s");
            var index = 0;
            var decoded = global::Dbus.Decoder.GetString(body, ref index);
            NameAcquired?.Invoke(decoded);
        }

        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($"Unexpected signature. Got ${ actual}, but expected ${ expected}");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }

    public sealed class OrgFreedesktopUpower : global::Dbus.Sample.IOrgFreedesktopUpower
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new global::System.Collections.Generic.List<System.IDisposable>();

        private OrgFreedesktopUpower(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            this.connection = connection;
            this.path = path ?? "/org/freedesktop/UPower";
            this.destination = destination ?? "org.freedesktop.UPower";

        }

        public static global::Dbus.Sample.IOrgFreedesktopUpower Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            return new OrgFreedesktopUpower(connection, path, destination);
        }


        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.IDictionary<global::System.String,global::System.Object>> GetAllAsync(global::System.String interfaceName)
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, interfaceName);

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
            var result = global::Dbus.Decoder.GetDictionary(receivedMessage.Body, ref index, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            return result;

        }


        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($"Unexpected signature. Got ${ actual}, but expected ${ expected}");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }

    public sealed class SampleObject_Proxy: global::System.IDisposable
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.Sample.SampleObject target;

        private global::System.IDisposable registration;

        private SampleObject_Proxy(global::Dbus.Connection connection, global::Dbus.Sample.SampleObject target, global::Dbus.ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            registration = connection.RegisterObjectProxy(
                path ?? "/org/dbuscore/sample",
                "org.dbuscore.sample.interface",
                handleMethodCall
            );
        }

        public static SampleObject_Proxy Factory(global::Dbus.Connection connection, Dbus.Sample.SampleObject target, global::Dbus.ObjectPath path)
        {
            return new SampleObject_Proxy(connection, target, path);
        }

        private System.Threading.Tasks.Task handleMethodCall(uint replySerial, global::Dbus.MessageHeader header, byte[] body)
        {
            switch (header.Member)
            {
                case "MyComplexMethod":
                    return handleMyComplexMethodAsync(replySerial, header, body);
                case "MyEcho":
                    return handleMyEchoAsync(replySerial, header, body);
                case "MyVoid":
                    return handleMyVoidAsync(replySerial, header, body);
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

        private async System.Threading.Tasks.Task handleMyComplexMethodAsync(uint replySerial, global::Dbus.MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "sii");
            var receiveIndex = 0;
            var p1 = global::Dbus.Decoder.GetString(receivedBody, ref receiveIndex);
            var p2 = global::Dbus.Decoder.GetInt32(receivedBody, ref receiveIndex);
            var p3 = global::Dbus.Decoder.GetInt32(receivedBody, ref receiveIndex);
            var result = await target.MyComplexMethodAsync(p1, p2, p3);
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, result.Item1);
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, result.Item2);
            await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody,"si");
        }

        private async System.Threading.Tasks.Task handleMyEchoAsync(uint replySerial, global::Dbus.MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "s");
            var receiveIndex = 0;
            var message = global::Dbus.Decoder.GetString(receivedBody, ref receiveIndex);
            var result = await target.MyEchoAsync(message);
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, result);
            await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody,"s");
        }

        private async System.Threading.Tasks.Task handleMyVoidAsync(uint replySerial, global::Dbus.MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "");
            await target.MyVoidAsync();
            var sendBody = global::Dbus.Encoder.StartNew();
            await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody,"");
        }


        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new global::Dbus.DbusException(
                    global::Dbus.DbusException.CreateErrorName("InvalidSignature"),
                    "Invalid signature"
                );
        }

        public void Dispose()
        {
            registration.Dispose();
        }
    }

}
