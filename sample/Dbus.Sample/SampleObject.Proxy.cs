using System;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    public class SampleObject_Proxy : IDisposable
    {
        private readonly Connection connection;
        private readonly SampleObject target;

        private IDisposable registration;

        public SampleObject_Proxy(Connection connection, SampleObject target)
        {
            this.connection = connection;
            this.target = target;
            registration = connection.RegisterObjectProxy(
                "/com/dbuscore/sample",
                "com.dbuscore.sample.interface",
                handleMethodCall
            );
        }

        private Task handleMethodCall(uint replySerial, MessageHeader header, byte[] body)
        {
            switch (header.Member)
            {
                case "MyVoid":
                    return callMyVoidAsync(replySerial, header, body);
                case "MyEcho":
                    return callMyEchoAsync(replySerial, header, body);
                case "MyComplexMethod":
                    return callMyComplexMethodAsync(replySerial, header, body);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }

        private async Task callMyVoidAsync(uint replySerial, MessageHeader header, byte[] receivedBod)
        {
            assertSignature(header.BodySignature, null);

            await target.MyVoidAsync();

            var sendBody = Encoder.StartNew();
            await connection.SendMethodReturnAsync(
                replySerial,
                header.Sender,
                sendBody,
                ""
            );
        }

        private async Task callMyEchoAsync(uint replySerial, MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "s");

            var index = 0;
            var message = Decoder.GetString(receivedBody, ref index);

            var result = await target.MyEchoAsync(message);

            var sendBody = Encoder.StartNew();
            index = 0;
            Encoder.Add(sendBody, ref index, result);

            await connection.SendMethodReturnAsync(
                replySerial,
                header.Sender,
                sendBody,
                "s"
            );
        }

        private async Task callMyComplexMethodAsync(uint replySerial, MessageHeader header, byte[] receivedBody)
        {
            assertSignature(header.BodySignature, "sii");

            var index = 0;
            var p1 = Decoder.GetString(receivedBody, ref index);
            var p2 = Decoder.GetInt32(receivedBody, ref index);
            var p3 = Decoder.GetInt32(receivedBody, ref index);

            var result = await target.MyComplexMethodAsync(p1, p2, p3);

            var sendBody = Encoder.StartNew();
            index = 0;
            Encoder.Add(sendBody, ref index, result.Item1);
            Encoder.Add(sendBody, ref index, result.Item2);

            await connection.SendMethodReturnAsync(
                replySerial,
                header.Sender,
                sendBody,
                "si"
            );
        }

        private static void assertSignature(string actual, string expected)
        {
            if (expected == null && !string.IsNullOrEmpty(actual) || actual != expected)
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
