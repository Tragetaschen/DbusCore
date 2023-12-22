namespace Dbus;

public class MethodCallOptions(
    MessageHeader messageHeader,
    bool noReplyExpected,
    uint replySerial
)
{
    public string Sender { get; } = messageHeader.Sender!;
    public ObjectPath Path { get; } = messageHeader.Path!;
    public string InterfaceName { get; } = messageHeader.InterfaceName!;
    public string Member { get; } = messageHeader.Member!;
    public uint ReplySerial { get; } = replySerial;
    public bool NoReplyExpected { get; } = noReplyExpected;
}
