namespace Dbus
{
    internal enum DbusHeaderType : byte
    {
        Invalid,
        Path,
        InterfaceName,
        Member,
        ErrorName,
        ReplySerial,
        Destination,
        Sender,
        Signature,
        UnixFds
    }
}
