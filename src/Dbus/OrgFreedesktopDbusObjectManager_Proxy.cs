using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public sealed class OrgFreedesktopDbusObjectManager_Proxy : IDisposable, IProxy
    {

        public string interfaceName { get; }

        private readonly Connection connection;
        private readonly IOrgFreedesktopDbusObjectManagerProvide target;
        private readonly ObjectPath path;

        private IDisposable registration;

        private OrgFreedesktopDbusObjectManager_Proxy(Connection connection, IOrgFreedesktopDbusObjectManagerProvide target, ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            this.path = path;
            interfaceName = "org.freedesktop.DBus.ObjectManager";
            registration = connection.RegisterObjectProxy(
                path ?? "",
                interfaceName,
                this
            );
        }

        public static OrgFreedesktopDbusObjectManager_Proxy Factory(Connection connection, IOrgFreedesktopDbusObjectManagerProvide target, ObjectPath path)
        {
            return new OrgFreedesktopDbusObjectManager_Proxy(connection, target, path);
        }

        public void Encode(List<byte> sendBody, ref int sendIndex)

        {
            Encoder.AddArray(sendBody, ref sendIndex, (List<byte> sendBody_e, ref int sendIndex_e) =>
            {
            });
        }

        public Task HandleMethodCallAsync(uint replySerial, MessageHeader header, byte[] body, bool doNotReply)
        {
            switch (header.Member)
            {
                case "GetManagedObjects":
                    return handleGetManagedObjectsAsync(replySerial, header, body, doNotReply);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

        private async Task handleGetManagedObjectsAsync(uint replySerial, MessageHeader header, byte[] receivedBody, bool shouldSendReply)
        {
            assertSignature(header.BodySignature, "");
            var managedObjects = await target.GetManagedObjectsAsync().ConfigureAwait(false);
            var sendBody = Encoder.StartNew();
            var sendIndex = 0;
            if (shouldSendReply)
            {
                Encoder.AddArray(sendBody, ref sendIndex, (List<byte> sendBody_e, ref int sendIndex_e) =>
                {
                    foreach (var managedObject in managedObjects)
                    {
                        Encoder.EnsureAlignment(sendBody_e, ref sendIndex_e, 8);
                        Encoder.Add(sendBody_e, ref sendIndex_e, managedObject.Key);
                        Encoder.AddArray(sendBody_e, ref sendIndex_e, (List<byte> sendBody_e_e, ref int sendIndex_e_e) =>
                        {
                            foreach (var interfaceInstance in managedObject.Value)
                            {
                                Encoder.EnsureAlignment(sendBody_e_e, ref sendIndex_e_e, 8);
                                Encoder.Add(sendBody_e, ref sendIndex_e_e, interfaceInstance.interfaceName);
                                interfaceInstance.EncodeProperties(sendBody_e_e, ref sendIndex);
                            }
                            Encoder.EnsureAlignment(sendBody_e_e, ref sendIndex_e_e, 8);
                            Encoder.Add(sendBody_e, ref sendIndex_e_e, "org.freedesktop.DBus.Properties");
                            Encoder.AddArray(sendBody_e_e, ref sendIndex_e_e, (List<byte> sendBody_e_e_e, ref int sendIndex_e_e_e) => { }, true);
                        }, true);

                    }
                }, true);
                await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, "a{oa{sa{sv}}}").ConfigureAwait(false);
            }
        }

        public void EncodeProperties(List<byte> sendBody, ref int index)
        {
            throw new DbusException(
                    DbusException.CreateErrorName("InvalidCall"),
                    "ObjectManager has no Properties"
                );
        }

        public void EncodeProperty(List<byte> sendBody, ref int index, string requestedProperty)
        {
            throw new DbusException(
                    DbusException.CreateErrorName("InvalidCall"),
                    "ObjectManager has no Properties"
                );
        }


        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new DbusException(
                    DbusException.CreateErrorName("InvalidSignature"),
                    "Invalid signature"
                );
        }

        public void Dispose()
        {
            registration.Dispose();
        }
    }
}
