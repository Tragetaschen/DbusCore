using System;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder consumeProperties(PropertyInfo[] properties, string interfaceName)
        {
            var builder = new StringBuilder()
                .Append(@"
        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public global::System.Threading.Tasks.Task PropertyInitializationFinished { get; }

        private void handleProperties(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature(""sa{sv}as"");
            var interfaceName = global::Dbus.Decoder.GetString(decoder);
            if (interfaceName != """)
                .Append(interfaceName)
                .Append(@""")
                return;
            var changed = global::Dbus.Decoder.GetDictionary(decoder, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            applyProperties(changed);
        }

        private async global::System.Threading.Tasks.Task initProperties(global::System.Threading.CancellationToken cancellationToken)
        {
            var sendBody = new global::Dbus.Encoder();
            sendBody.Add(""")
                .Append(interfaceName)
                .Append(@""");

            var decoder = await connection.SendMethodCall(
                this.path,
                ""org.freedesktop.DBus.Properties"",
                ""GetAll"",
                this.destination,
                sendBody,
                ""s"",
                cancellationToken
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature(""a{sv}"");
                var properties = global::Dbus.Decoder.GetDictionary(
                    decoder,
                    global::Dbus.Decoder.GetString,
                    global::Dbus.Decoder.GetObject
                );
                applyProperties(properties);
            }
        }

        private void applyProperties(global::System.Collections.Generic.IDictionary<string, object> changed)
        {
            foreach (var entry in changed)
            {
                switch (entry.Key)
                {");
            foreach (var property in properties)
            {
                if (property.SetMethod != null)
                    throw new InvalidOperationException("Cache properties can only have getters");
                builder
                    .Append(@"
                    case """)
                    .Append(property.Name)
                    .Append(@""":
                        ")
                    .Append(property.Name)
                    .Append(" = (")
                    .Append(BuildTypeString(property.PropertyType))
                    .Append(@")entry.Value;
                        break;")
                ;
            }
            builder.AppendLine(@"
                }
                PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(entry.Key));
            }
        }");
            foreach (var property in properties)
                builder
                    .Append(@"
        public ")
                    .Append(BuildTypeString(property.PropertyType))
                    .Append(" ")
                    .Append(property.Name)
                    .Append(" { get; private set; }")
                ;

            builder.AppendLine();
            return builder;
        }
    }
}
