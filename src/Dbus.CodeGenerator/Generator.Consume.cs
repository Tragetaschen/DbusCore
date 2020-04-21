using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder consume(Type type, DbusConsumeAttribute dbusConsumeAttribute)
        {
            if (!typeof(IDisposable).IsAssignableFrom(type))
                throw new InvalidOperationException("The interface " + type.Name + " is meant to be consumed, but does not extend IDisposable");
            if (!typeof(IAsyncDisposable).IsAssignableFrom(type))
                throw new InvalidOperationException("The interface " + type.Name + " is meant to be consumed, but does not extend IAsyncDisposable");

            var className = type.Name + "_Implementation";

            var properties = type.GetProperties();
            if (properties.Length > 0)
            {
                if (!typeof(IDbusPropertyInitialization).IsAssignableFrom(type))
                    throw new InvalidOperationException("Interface " + type.Name + " with cache properties does not implement 'IDbusPropertyInitialization'");
                if (!typeof(INotifyPropertyChanged).IsAssignableFrom(type))
                    throw new InvalidOperationException("Interface " + type.Name + " with cache properties does not implement 'INotifyPropertyChanged'");
            }

            var build = new StringBuilder()
                .Append(@"
    public sealed class ")
                .Append(className)
                .Append(" : ")
                .Append(BuildTypeString(type))
                .Append(@"
    {")
                .Append(consumeSkeleton(dbusConsumeAttribute.InterfaceName))
                .Append(consumeConstructor(
                    className,
                    dbusConsumeAttribute,
                    properties,
                    type.GetEvents()
                ))
            ;
            if (properties.Length > 0)
                build.Append(consumeProperties(properties, dbusConsumeAttribute.InterfaceName));
            foreach (var methodInfo in type.GetMethods().OrderBy(x => x.Name))
                if (!methodInfo.IsSpecialName)
                    build.Append(consumeMethod(methodInfo, dbusConsumeAttribute.InterfaceName));

            foreach (var eventInfo in type.GetEvents().OrderBy(x => x.Name))
                build.Append(consumeEventImplementation(eventInfo));
            build.AppendLine(@"
    }");
            return build;
        }
    }
}
