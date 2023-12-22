using System;

namespace Dbus;

[Flags]
internal enum DbusMessageFlags : byte
{
    None = 0,
    NoReplyExpected = 0x1,
    NoAutoStart = 0x2,
    AllowInteractiveAuthorization = 0x4,
}
