using System;
using System.ComponentModel;
using System.Text;

namespace Dbus.CodeGenerator
{
    public partial class Generator
    {
        private static StringBuilder provideSkeleton(Type type, DbusProvideAttribute provide)
        {
            var builder = new StringBuilder();
            builder
                .Append(@"
        private readonly global::Dbus.Connection connection;
        private readonly ")
                .Append(BuildTypeString(type))
                .Append(@" target;
        private readonly global::Dbus.ObjectPath path;
        private readonly global::System.IDisposable registration;

        private ")
                .Append(type.Name)
                .Append(@"_Proxy(global::Dbus.Connection connection, ")
                .Append(BuildTypeString(type))
                .Append(@" target, global::Dbus.ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            this.path = path;
            InterfaceName = """)
                .Append(provide.InterfaceName)
                .Append(@""";
            registration = connection.RegisterObjectProxy(
                path ?? """)
                .Append(provide.Path)
                .Append(@""",
                InterfaceName,
                this
            );");

            if (typeof(INotifyPropertyChanged).IsAssignableFrom(type))
                builder.Append(@"

            target.PropertyChanged += handlePropertyChangedEventAsync;");

            builder
                .Append(@"
        }

        public object Target => target;
        public string InterfaceName { get; }

        public override string ToString()
        {
            return this.InterfaceName + ""@"" + this.path;
        }

        public static ")
                .Append(type.Name)
                .Append(@"_Proxy Factory(global::Dbus.Connection connection, ")
                .Append(type.FullName)
                .Append(@" target, global::Dbus.ObjectPath path)
        {
            return new ")
                .Append(type.Name)
                .AppendLine(@"_Proxy(connection, target, path);
        }

        public void Dispose()
        {
            registration.Dispose();
        }");

            return builder;
        }
    }
}
