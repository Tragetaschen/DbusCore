using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public sealed class OrgFreedesktopDbusObjectManager_Proxy : IProxy
    {
        public string InterfaceName { get; }

        private readonly Connection connection;
        private readonly IOrgFreedesktopDbusObjectManagerProvide target;
        private readonly ObjectPath path;

        private IDisposable registration;

        private OrgFreedesktopDbusObjectManager_Proxy(Connection connection, IOrgFreedesktopDbusObjectManagerProvide target, ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            this.path = path ?? throw new ArgumentNullException(nameof(path));
            InterfaceName = "org.freedesktop.DBus.ObjectManager";
            registration = connection.RegisterObjectProxy(
                path,
                InterfaceName,
                this
            );
        }


        public static OrgFreedesktopDbusObjectManager_Proxy Factory(Connection connection, IOrgFreedesktopDbusObjectManagerProvide target, ObjectPath path)
            => new OrgFreedesktopDbusObjectManager_Proxy(connection, target, path);

        public object Target => target;

        public void Encode(Encoder encoder)
            => encoder.Add(0); // empty array

        public Task HandleMethodCallAsync(
            MethodCallOptions methodCallOptions,
            ReceivedMessage message,
            CancellationToken cancellationToken
        )
        {
            switch (methodCallOptions.Member)
            {
                case "GetManagedObjects":
                    return handleGetManagedObjectsAsync(methodCallOptions, message, cancellationToken);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

        private async Task handleGetManagedObjectsAsync(
            MethodCallOptions methodCallOptions,
            ReceivedMessage message,
            CancellationToken cancellationToken
        )
        {
            message.AssertSignature("");
            var managedObjects = await target.GetManagedObjectsAsync(cancellationToken).ConfigureAwait(false);
            var sendBody = new Encoder();
            if (!methodCallOptions.ShouldSendReply)
                return;
            sendBody.AddArray(() =>
            {
                foreach (var managedObject in managedObjects)
                {
                    sendBody.StartCompoundValue();
                    sendBody.Add(managedObject.Key);
                    sendBody.AddArray(() =>
                    {
                        foreach (var interfaceInstance in managedObject.Value)
                        {
                            sendBody.StartCompoundValue();
                            sendBody.Add(interfaceInstance.InterfaceName);
                            interfaceInstance.EncodeProperties(sendBody);
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
