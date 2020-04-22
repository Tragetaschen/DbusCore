using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder consumeConstructor(
            string className,
            DbusConsumeAttribute dbusConsumeAttribute,
            PropertyInfo[] properties,
            EventInfo[] events
        )
        {
            var builder = new StringBuilder()
                .Append(@"
        private ")
                .Append(className)
                .Append(@"(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            this.connection = connection;
            this.path = path ?? """)
                .Append(dbusConsumeAttribute.Path)
                .Append(@""";
            this.destination = destination ?? """)
                .Append(dbusConsumeAttribute.Destination)
                .Append(@""";");

            if (properties.Length > 0)
                builder.Append(@"
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                ""org.freedesktop.DBus.Properties"",
                ""PropertiesChanged"",
                (global::Dbus.Connection.SignalHandler)this.handleProperties
            ));
            PropertyInitializationFinished = initProperties(cancellationToken);");

            foreach (var eventInfo in events.OrderBy(x => x.Name))
                builder.Append(consumeEventSubscription(eventInfo, dbusConsumeAttribute.InterfaceName));
            builder.Append(@"
        }

        public static ")
                .Append(className)
                .Append(@" Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            return new ")
                .Append(className)
                .AppendLine(@"(connection, path, destination, cancellationToken);
        }");

            return builder;
        }
    }
}
