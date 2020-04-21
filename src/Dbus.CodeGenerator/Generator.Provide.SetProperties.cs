using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder provideSetProperties(PropertyInfo[] properties)
        {
            var builder = new StringBuilder();
            builder.Append(@"
        public void SetProperty(string propertyName, global::Dbus.Decoder decoder)
        {
            switch (propertyName)
            {");
            foreach (var property in properties)
            {
                if (property.SetMethod == null)
                    continue;
                builder
                    .Append(@"
                case """)
                    .Append(property.Name)
                    .Append(@""":
                    decodeAndSet")
                    .Append(property.Name)
                    .Append(@"(decoder);
                    break;")
                ;
            }
            builder.AppendLine(@"
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownProperty""),
                        ""No such Property: "" + propertyName
                    );
            }
        }");

            foreach (var property in properties)
            {
                if (property.SetMethod == null)
                    continue;
                var decoder = DecoderGenerator.Create(property.Name, property.PropertyType);
                builder
                    .Append(decoder.Delegates)
                    .Append(@"
        private void decodeAndSet")
                    .Append(property.Name)
                    .Append(@"(global::Dbus.Decoder decoder)
        {
            var signature = global::Dbus.Decoder.GetSignature(decoder);
            signature.AssertEqual(""")
                    .Append(decoder.Signature)
                    .Append(@""");
            var value = ")
                    .Append(decoder.DelegateName)
                    .Append(@"(decoder);
            target.")
                    .Append(property.Name)
                    .AppendLine(@" = value;
        }");
            }
            return builder;
        }
    }
}
