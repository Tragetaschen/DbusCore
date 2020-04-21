using System;
using System.Linq;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        public static StringBuilder BuildTypeString(Type type)
        {
            if (!type.IsConstructedGenericType)
                return new StringBuilder()
                    .Append("global::")
                    .Append(type.FullName)
                ;

            var genericName = type.GetGenericTypeDefinition().FullName ?? throw new InvalidOperationException("Now full name");
            var withoutSuffix = genericName[0..^2];
            return new StringBuilder()
                .Append("global::")
                .Append(withoutSuffix)
                .Append("<")
                .AppendJoin(", ", type.GenericTypeArguments.Select(BuildTypeString))
                .Append(">")
            ;
        }
    }
}
