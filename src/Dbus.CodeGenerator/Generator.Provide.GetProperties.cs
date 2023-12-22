using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator;

public static partial class Generator
{
    private static StringBuilder provideGetProperties(PropertyInfo[] properties)
    {
        var builder = new StringBuilder()
            .Append(@"
        public void EncodeProperties(global::Dbus.Encoder sendBody)
        {
            var state = sendBody.StartArray(storesCompoundValues: true);
");

        foreach (var property in properties)
            builder
                .Append(@"
            sendBody.StartCompoundValue();
            sendBody.Add(""")
                .Append(property.Name)
                .Append(@""");
            encode")
                .Append(property.Name)
                .AppendLine(@"(sendBody);");

        builder.AppendLine(@"
            sendBody.FinishArray(state);
        }

        public void EncodeProperty(global::Dbus.Encoder sendBody, string propertyName)
        {
            switch (propertyName)
            {");

        foreach (var property in properties)
            builder
                .Append(Indent)
                .Append(@"    case """)
                .Append(property.Name)
                .Append(@""":
                    encode")
                .Append(property.Name)
                .AppendLine(@"(sendBody);
                    break;")
            ;

        builder
            .Append(Indent)
            .AppendLine(@"    default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownProperty""),
                        ""No such Property: "" + propertyName
                    );
            }
        }");

        foreach (var property in properties)
        {
            var encoder = new EncoderGenerator("sendBody");
            encoder.AddVariant("value", property.PropertyType);
            builder
                .Append(@"
        private void encode")
                .Append(property.Name)
                .Append(@"(global::Dbus.Encoder sendBody)
        {
            var value = target.")
                .Append(property.Name)
                .AppendLine(@";")
                .Append(encoder.Result)
                .AppendLine(@"        }")
            ;
        }

        return builder;
    }
}
