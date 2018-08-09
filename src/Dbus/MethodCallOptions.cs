namespace Dbus
{
    public class MethodCallOptions
    {
        public MethodCallOptions(
            MessageHeader messageHeader,
            bool shoudSendReply,
            uint replySerial
        )
        {
            Sender = messageHeader.Sender;
            Path = messageHeader.Path;
            InterfaceName = messageHeader.InterfaceName;
            Member = messageHeader.Member;
            ReplySerial = replySerial;
            ShouldSendReply = shoudSendReply;
        }

        public string Sender { get; }
        public ObjectPath Path { get; }
        public string InterfaceName { get; }
        public string Member { get; }
        public uint ReplySerial { get; }
        public bool ShouldSendReply { get; }
    }
}
