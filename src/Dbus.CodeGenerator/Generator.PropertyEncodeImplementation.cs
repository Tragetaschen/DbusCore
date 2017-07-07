using System;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static string generatePropertyEncodeImplementation(Type type)
        {
            var propertyEncoder = new StringBuilder();
            propertyEncoder.Append(@"
        public void EncodeProperties(global::System.Collections.Generic.List<byte> sendBody, ref int sendIndex)
        {
            global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e, ref int sendIndex_e) =>
            {");

            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                propertyEncoder.Append(@"
                global::Dbus.Encoder.EnsureAlignment(sendBody_e, ref sendIndex_e, 8);
                global::Dbus.Encoder.Add(sendBody_e, ref sendIndex_e, """ + property.Name + @""");
                Encode" + property.Name + @"(sendBody_e, ref sendIndex_e);");
            }
            propertyEncoder.Append(@"
            }, true);
        }

        public void EncodeProperty(global::System.Collections.Generic.List<byte> sendBody, ref int sendIndex, string propertyName)
        {
            switch (propertyName)
            {");
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                propertyEncoder.Append(@"
                case """ + property.Name + @""":
                    Encode" + property.Name + @"(sendBody, ref sendIndex);
                    break;");
            }
            propertyEncoder.Append(@"
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownProperty""),
                        ""No such Property: "" + propertyName
                    );
            }
        }");
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                var encoder = new EncoderGenerator("sendBody", "sendIndex");
                encoder.AddVariant("value", property.PropertyType);
                propertyEncoder.Append(@"

        private void Encode" + property.Name + @"(global::System.Collections.Generic.List<byte> sendBody, ref int sendIndex)
        {
            var value = target." + property.Name + @";
" + encoder.Result + @"
        }");
            }
            return propertyEncoder.ToString();
        }
    }
}
