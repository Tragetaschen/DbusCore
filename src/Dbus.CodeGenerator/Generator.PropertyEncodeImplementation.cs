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
        public void EncodeProperties(global::Dbus.Encoder sendBody)
        {
            var state = sendBody.StartArray(storesCompoundValues: true);
");

            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                propertyEncoder.Append(@"
            sendBody.StartCompoundValue();
            sendBody.Add(""" + property.Name + @""");
            encode" + property.Name + @"(sendBody);
");
            }
            propertyEncoder.Append(@"
            sendBody.FinishArray(state);
        }

        public void EncodeProperty(global::Dbus.Encoder sendBody, string propertyName)
        {
            switch (propertyName)
            {");
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                propertyEncoder.Append(@"
                case """ + property.Name + @""":
                    encode" + property.Name + @"(sendBody);
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
                var encoder = new EncoderGenerator("sendBody");
                encoder.AddVariant("value", property.PropertyType);
                propertyEncoder.Append(@"

        private void encode" + property.Name + @"(global::Dbus.Encoder sendBody)
        {
            var value = target." + property.Name + @";
" + encoder.Result + @"
        }");
            }
            return propertyEncoder.ToString();
        }
    }
}
