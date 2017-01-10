namespace WebApplication.Dbus
{

    public static partial class DbusImplementations
    {
        static partial void DoAddDbus(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.IOrgFreedesktopDbus>(OrgFreedesktopDbus.Factory);
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, serviceProvider =>
            {
                var options = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::Microsoft.Extensions.Options.IOptions<global::Dbus.DbusConnectionOptions>>(serviceProvider);
                return global::Dbus.Connection.CreateAsync(options.Value);
            });
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, async serviceProvider =>
            {
                var connection = await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::System.Threading.Tasks.Task<global::Dbus.Connection>>(serviceProvider);
                return connection.Consume<global::Dbus.IOrgFreedesktopDbus>();
            });
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

}
