using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dbus
{
    public class ReceivedMessage : IDisposable
    {
        private readonly MessageHeader messageHeader;

        public ReceivedMessage(
            MessageHeader messageHeader,
            Decoder decoder
        )
        {
            this.messageHeader = messageHeader;
            Decoder = decoder;
        }

        public Decoder Decoder { get; }

        public void AssertSignature(Signature expectedSignature)
            => messageHeader.BodySignature!.AssertEqual(expectedSignature);

        public void Dispose() => Decoder.Dispose();
    }
}
