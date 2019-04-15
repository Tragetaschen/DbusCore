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

        public SafeHandle[] UnixFds => messageHeader.UnixFds;
        public Stream GetStream(int index) => messageHeader.GetStream(index);
        public Decoder Decoder { get; }

        public void AssertSignature(Signature signature)
            => signature.AssertEqual(messageHeader.BodySignature);

        public void Dispose() => Decoder.Dispose();
    }
}
