using System;
using System.Collections.Generic;

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
            [typeof(double)] = "d",
        };
    }
}