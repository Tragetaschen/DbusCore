namespace Dbus;

internal struct DbusFixedLengthHeader
{
    public DbusEndianess Endianess;
    public DbusMessageType MessageType;
    public DbusMessageFlags Flags;
    public DbusProtocolVersion ProtocolVersion;
    public int BodyLength;
    public uint Serial;
    public int ArrayLength;
}
