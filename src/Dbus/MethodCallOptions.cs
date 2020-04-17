namespace Dbus
{
    public class MethodCallOptions
    {
        public MethodCallOptions(
            MessageHeader messageHeader,
            bool noReplyExpected,
            uint replySerial
        )
        {
            Sender = messageHeader.Sender!;
            Path = messageHeader.Path!;
            InterfaceName = messageHeader.InterfaceName!;
            Member = messageHeader.Member!;
            ReplySerial = replySerial;
            NoReplyExpected = noReplyExpected;
        }

        public string Sender { get; }
        public ObjectPath Path { get; }
        public string InterfaceName { get; }
        public string Member { get; }
        public uint ReplySerial { get; }
        public bool NoReplyExpected { get; }
    }
}
