using System;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Dbus.CodeGenerator;

public static partial class Generator
{
    private static StringBuilder provide(Type type, DbusProvideAttribute dbusProvideAttribute)
    {
        var builder = new StringBuilder()
            .Append(@"
    public sealed class ")
            .Append(type.Name)
            .Append(@"_Proxy : global::Dbus.IProxy
    {")
            .Append(provideSkeleton(type, dbusProvideAttribute))
        ;
        if (typeof(INotifyPropertyChanged).IsAssignableFrom(type))
            builder.Append(provideINotifyPropertyChanged(type, dbusProvideAttribute.InterfaceName));

        builder
            .Append(provideGetProperties(type.GetProperties()))
            .Append(provideSetProperties(type.GetProperties()))
            .Append(provideMethods(type
                .GetMethods()
                .Where(x => !x.IsSpecialName)
                .Where(x => x.DeclaringType != typeof(object))
                .OrderBy(x => x.Name)
                .ToArray()
            ))
            .AppendLine(@"
    }");

        return builder;
    }
}
