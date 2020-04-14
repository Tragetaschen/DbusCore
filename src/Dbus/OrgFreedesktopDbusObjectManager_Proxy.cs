using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public sealed class OrgFreedesktopDbusObjectManager_Proxy : IProxy
    {
        private readonly Connection connection;
        private readonly IOrgFreedesktopDbusObjectManagerProvide target;
        private readonly IDisposable registration;

        private OrgFreedesktopDbusObjectManager_Proxy(Connection connection, IOrgFreedesktopDbusObjectManagerProvide target, ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            registration = connection.RegisterObjectProxy(
                path ?? throw new ArgumentNullException(nameof(path)),
                InterfaceName,
                this
            );
        }

        public static OrgFreedesktopDbusObjectManager_Proxy Factory(Connection connection, IOrgFreedesktopDbusObjectManagerProvide target, ObjectPath path)
            => new OrgFreedesktopDbusObjectManager_Proxy(connection, target, path);

        public string InterfaceName => "org.freedesktop.DBus.ObjectManager";
        public object Target => target;

        public Task HandleMethodCallAsync(
            MethodCallOptions methodCallOptions,
            Decoder decoder,
            CancellationToken cancellationToken
        )
        {
            switch (methodCallOptions.Member)
            {
                case "GetManagedObjects":
                    return handleGetManagedObjectsAsync(methodCallOptions, decoder, cancellationToken);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

        private async Task handleGetManagedObjectsAsync(
            MethodCallOptions methodCallOptions,
            Decoder decoder,
            CancellationToken cancellationToken
        )
        {
            decoder.AssertSignature("");
            var managedObjects = await target.GetManagedObjectsAsync(cancellationToken).ConfigureAwait(false);
            if (!methodCallOptions.ShouldSendReply)
                return;
            var sendBody = new Encoder();
            sendBody.AddArray(() =>
            {
                foreach (var managedObject in managedObjects)
                {
                    sendBody.StartCompoundValue();
                    sendBody.Add(managedObject.Key);
                    sendBody.AddArray(() =>
                    {
                        foreach (var proxy in managedObject.Value)
                        {
                            sendBody.StartCompoundValue();
                            sendBody.Add(proxy.InterfaceName);
                            proxy.EncodeProperties(sendBody);
                        }
                        sendBody.StartCompoundValue();
                        sendBody.Add("org.freedesktop.DBus.Properties");
                        sendBody.AddArray(() => { }, storesCompoundValues: true);  // empty properties for the properties interface
                    }, storesCompoundValues: true);
                }
            }, storesCompoundValues: true);
            await connection.SendMethodReturnAsync(
                methodCallOptions,
                sendBody,
                "a{oa{sa{sv}}}",
                cancellationToken
            ).ConfigureAwait(false);
        }

        public void EncodeProperties(Encoder encoder)
            => throw new DbusException(
                DbusException.CreateErrorName("InvalidCall"),
                "ObjectManager has no Properties"
            );

        public void EncodeProperty(Encoder encoder, string requestedProperty)
            => throw new DbusException(
                DbusException.CreateErrorName("InvalidCall"),
                "ObjectManager has no Properties"
            );

        public void Dispose() => registration.Dispose();
    }
}
