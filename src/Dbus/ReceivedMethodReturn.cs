using System.Buffers;

namespace Dbus
{
    public struct ReceivedMethodReturn
    {
        public MessageHeader Header;
        public Signature Signature;
        public IMemoryOwner<byte> Body;
        public int BodyLength;
    }
}
