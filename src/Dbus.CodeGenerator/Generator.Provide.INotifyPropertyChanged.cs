using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator;

public partial class Generator
{
    private static StringBuilder provideINotifyPropertyChanged(Type type, string interfaceName)
    {
        var builder = new StringBuilder();
        var properties = type.GetProperties().Where(x => x.GetCustomAttribute<DbusPropertiesChanged>() != null);

        if (!properties.Any())
            throw new InvalidOperationException(type + " implements INotifyPropertyChanged, but no property has the DbusPropertiesChangedAttribute");

        //Only one property is changed at a time, so no loop is necessary
        //Invalidated properties are not supported, so the array is encoded as 0 integer
        builder
            .Append(@"
        private void encodeChangedProperty(global::Dbus.Encoder sendBody, string propertyName)
        {
            sendBody.Add(""")
            .Append(interfaceName)
            .Append(@""");
            var state = sendBody.StartArray(storesCompoundValues: true);
            sendBody.StartCompoundValue();
            sendBody.Add(propertyName);
            switch (propertyName)
            {")
        ;

        foreach (var property in properties)
            builder
                .Append(@"
                case """)
                .Append(property.Name)
                .Append(@""":
                    encode")
                .Append(property.Name)
                .AppendLine(@"(sendBody);
                    break;")
            ;

        builder
            .AppendLine(@"
                default:
                    throw new global::System.NotSupportedException(""Property "" + propertyName + "" does not support change notifications. Please add the DbusPropertiesChangedAttribute"");
            }
            sendBody.FinishArray(state);
            sendBody.Add(0);
        }

        private async void handlePropertyChangedEventAsync(object sender, global::System.ComponentModel.PropertyChangedEventArgs e)
        {
            var sendBody = new global::Dbus.Encoder();
            encodeChangedProperty(sendBody, e.PropertyName);

            await connection.SendSignalAsync(
                path,
                ""org.freedesktop.DBus.Properties"",
                ""PropertiesChanged"",
                sendBody,
                ""sa{sv}as"",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
        }");

        return builder;
    }
}
