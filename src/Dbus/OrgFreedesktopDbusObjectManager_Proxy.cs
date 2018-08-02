using System;
using System.Collections.Generic;
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

        public void Encode(Encoder encoder)
            => encoder.Add(0); // empty array

        public Task HandleMethodCallAsync(uint replySerial, MessageHeader header, Decoder decoder, bool doNotReply)
        {
            switch (header.Member)
            {
                case "GetManagedObjects":
                    return handleGetManagedObjectsAsync(replySerial, header, doNotReply);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

        private async Task handleGetManagedObjectsAsync(uint replySerial, MessageHeader header, bool shouldSendReply)
        {
            header.BodySignature.AssertEqual("");
            var managedObjects = await target.GetManagedObjectsAsync().ConfigureAwait(false);
            var sendBody = new Encoder();
            if (shouldSendReply)
            {
                sendBody.AddArray(() =>
                {
                    foreach (var managedObject in managedObjects)
                    {
                        sendBody.EnsureAlignment(8);
                        sendBody.Add(managedObject.Key);
                        sendBody.AddArray(() =>
                        {
                            foreach (var interfaceInstance in managedObject.Value)
                            {
                                sendBody.EnsureAlignment(8);
                                sendBody.Add(interfaceInstance.InterfaceName);
                                interfaceInstance.EncodeProperties(sendBody);
                            }
                            sendBody.EnsureAlignment(8);
                            sendBody.Add("org.freedesktop.DBus.Properties");
                            sendBody.Add(0); // empty properties for the properties interface
                            sendBody.EnsureAlignment(8);
                        }, true);

                    }
                }, true);
                await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, "a{oa{sa{sv}}}").ConfigureAwait(false);
            }
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
