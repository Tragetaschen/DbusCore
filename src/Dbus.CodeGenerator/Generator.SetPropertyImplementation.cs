using System;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static string generateSetPropertyImplementation(Type type)
        {
            var propertyDecoder = new StringBuilder();
            propertyDecoder.Append(@"
        public void SetProperty(string propertyName, global::Dbus.Decoder decoder)
        {
            switch (propertyName)
            {");
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                if (property.SetMethod == null)
                    continue;
                propertyDecoder.Append(@"
                case """ + property.Name + @""":
                    decodeAndSet" + property.Name + @"(decoder);
                    break;");
            }
            propertyDecoder.Append(@"
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownProperty""),
                        ""No such Property: "" + propertyName
                    );
            }
        }
");
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                if (property.SetMethod == null)
                    continue;
                var decoder = DecoderGenerator.Create(property.Name, property.PropertyType);
                propertyDecoder.AppendJoin("", decoder.Delegates);
                propertyDecoder.Append(@"
        private void decodeAndSet" + property.Name + @"(global::Dbus.Decoder decoder)
        {
            var signature = global::Dbus.Decoder.GetSignature(decoder);
            signature.AssertEqual(""" + decoder.Signature + @""");
            var value = " + decoder.DelegateName + @"(decoder);
            target." + property.Name + @" = value;
        }");
            }
            return propertyDecoder.ToString();
        }
    }
}
