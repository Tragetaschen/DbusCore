using System;
using System.Collections.Generic;
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

        private Encoder encodeManagedObjects(IDictionary<ObjectPath, List<IProxy>> managedObjects)
        {
            var encoder = new Encoder();
            var dictionaryState = encoder.StartArray(storesCompoundValues: true);

            foreach (var managedObject in managedObjects)
            {
                encoder.StartCompoundValue();
                encoder.Add(managedObject.Key);
                var objectState = encoder.StartArray(storesCompoundValues: true);
                foreach (var proxy in managedObject.Value)
                {
                    encoder.StartCompoundValue();
                    encoder.Add(proxy.InterfaceName);
                    proxy.EncodeProperties(encoder);
                }

                encoder.StartCompoundValue();
                encoder.Add("org.freedesktop.DBus.Properties");
                var emptyState = encoder.StartArray(storesCompoundValues: true);
                // empty properties for the properties interface
                encoder.FinishArray(emptyState);

                encoder.FinishArray(objectState);
            }

            encoder.FinishArray(dictionaryState);

            return encoder;
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
            var encoder = encodeManagedObjects(managedObjects);
            await connection.SendMethodReturnAsync(
                methodCallOptions,
                encoder,
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
