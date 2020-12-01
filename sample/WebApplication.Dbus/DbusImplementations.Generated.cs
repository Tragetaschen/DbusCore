namespace WebApplication.Dbus
{

    public static partial class DbusImplementations
    {
        private static void initRegistrations()
        {
            global::Dbus.Connection.AddPublishProxy<global::Dbus.IOrgFreedesktopDbusObjectManagerProvide>((global::System.Func<global::Dbus.Connection, global::Dbus.IOrgFreedesktopDbusObjectManagerProvide, global::Dbus.ObjectPath, global::Dbus.IProxy>)global::Dbus.OrgFreedesktopDbusObjectManager_Proxy.Factory);
            global::Dbus.Connection.AddConsumeImplementation<global::Dbus.IOrgFreedesktopDbusObjectManager>((global::System.Func<global::Dbus.Connection, global::Dbus.ObjectPath, string, global::System.Threading.CancellationToken, object>)IOrgFreedesktopDbusObjectManager_Implementation.Factory);
        }

        static partial void DoAddDbus(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            initRegistrations();
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, serviceProvider =>
            {
                var options = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::Microsoft.Extensions.Options.IOptions<global::Dbus.DbusConnectionOptions>>(serviceProvider);
                return global::Dbus.Connection.CreateAsync(options.Value);
            });
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, async serviceProvider =>
            {
                var connection = await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::System.Threading.Tasks.Task<global::Dbus.Connection>>(serviceProvider).ConfigureAwait(false);
                return connection.Consume<global::Dbus.IOrgFreedesktopDbusObjectManager>();
            });
        }
    }

    public sealed class IOrgFreedesktopDbusObjectManager_Implementation : global::Dbus.IOrgFreedesktopDbusObjectManager
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<global::System.IAsyncDisposable> eventSubscriptions = new global::System.Collections.Generic.List<global::System.IAsyncDisposable>();

        public override string ToString()
        {
            return "org.freedesktop.DBus.ObjectManager@" + this.path;
        }

        public void Dispose() => global::System.Threading.Tasks.Task.Run((global::System.Func<global::System.Threading.Tasks.ValueTask>)DisposeAsync);

        public async global::System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (var eventSubscription in eventSubscriptions)
                await eventSubscription.DisposeAsync();
        }

        private IOrgFreedesktopDbusObjectManager_Implementation(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            this.connection = connection;
            this.path = path ?? "";
            this.destination = destination ?? "";
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus.ObjectManager",
                "InterfacesAdded",
                (global::Dbus.Connection.SignalHandler)this.handleInterfacesAdded
            ));
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                "org.freedesktop.DBus.ObjectManager",
                "InterfacesRemoved",
                (global::Dbus.Connection.SignalHandler)this.handleInterfacesRemoved
            ));
        }

        public static IOrgFreedesktopDbusObjectManager_Implementation Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            return new IOrgFreedesktopDbusObjectManager_Implementation(connection, path, destination, cancellationToken);
        }

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>> decode_result_GetManagedObjectsAsync_v_v = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>> decode_result_GetManagedObjectsAsync_v = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, decode_result_GetManagedObjectsAsync_v_v);

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::Dbus.ObjectPath, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>>> decode_result_GetManagedObjectsAsync = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetObjectPath, decode_result_GetManagedObjectsAsync_v);

        public async global::System.Threading.Tasks.Task<global::System.Collections.Generic.Dictionary<global::Dbus.ObjectPath, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>>> GetManagedObjectsAsync(global::System.Threading.CancellationToken cancellationToken)
        {
            var decoder = await connection.SendMethodCall(
                this.path,
                "org.freedesktop.DBus.ObjectManager",
                "GetManagedObjects",
                this.destination,
                null,
                "",
                cancellationToken
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature("a{oa{sa{sv}}}");
                return decode_result_GetManagedObjectsAsync(decoder);
            }
        }

        public event global::System.Action<global::Dbus.ObjectPath, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>> InterfacesAdded;

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>> decode_decoded1_InterfacesAdded_v = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.Dictionary<global::System.String, global::System.Collections.Generic.Dictionary<global::System.String, global::System.Object>>> decode_decoded1_InterfacesAdded = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, decode_decoded1_InterfacesAdded_v);

        private void handleInterfacesAdded(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature("oa{sa{sv}}");
            var decoded0_InterfacesAdded = global::Dbus.Decoder.GetObjectPath(decoder);
            var decoded1_InterfacesAdded = decode_decoded1_InterfacesAdded(decoder);
            InterfacesAdded?.Invoke(decoded0_InterfacesAdded, decoded1_InterfacesAdded);
        }

        public event global::System.Action<global::Dbus.ObjectPath, global::System.Collections.Generic.List<global::System.String>> InterfacesRemoved;

        private static readonly global::Dbus.Decoder.ElementDecoder<global::System.Collections.Generic.List<global::System.String>> decode_decoded1_InterfacesRemoved = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetArray(decoder, global::Dbus.Decoder.GetString, false);

        private void handleInterfacesRemoved(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature("oas");
            var decoded0_InterfacesRemoved = global::Dbus.Decoder.GetObjectPath(decoder);
            var decoded1_InterfacesRemoved = decode_decoded1_InterfacesRemoved(decoder);
            InterfacesRemoved?.Invoke(decoded0_InterfacesRemoved, decoded1_InterfacesRemoved);
        }

    }

}
