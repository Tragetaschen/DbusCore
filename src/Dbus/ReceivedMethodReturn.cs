namespace Dbus
{
    public struct ReceivedMethodReturn
    {
        public MessageHeader Header;
        public Signature Signature;
        public byte[] Body;
    }
}
