namespace Dbus
{
    internal enum DbusMessageType : byte
    {
        Invalid,
        MethodCall,
        MethodReturn,
        Error,
        Signal,
    }
}
