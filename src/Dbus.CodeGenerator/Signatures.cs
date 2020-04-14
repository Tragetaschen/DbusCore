using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Dbus.CodeGenerator
{
    public static class SignatureString
    {
        public static readonly Dictionary<Type, string> For = new Dictionary<Type, string>()
        {
            [typeof(ObjectPath)] = "o",
            [typeof(string)] = "s",
            [typeof(Signature)] = "g",
            [typeof(byte)] = "y",
            [typeof(bool)] = "b",
            [typeof(short)] = "n",
            [typeof(ushort)] = "q",
            [typeof(int)] = "i",
            [typeof(uint)] = "u",
            [typeof(object)] = "v",
            [typeof(long)] = "x",
            [typeof(ulong)] = "t",
            [typeof(double)] = "d",
            [typeof(SafeHandle)] = "h",
            [typeof(Stream)] = "h",
        };
    }
}
