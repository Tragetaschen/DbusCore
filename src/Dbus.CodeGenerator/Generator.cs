using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        public const string Indent = "            ";

        public static string Run(Func<List<StringBuilder>, StringBuilder>? registerServices = null)
        {
            var entry = Assembly.GetEntryAssembly();
            if (entry == null)
                throw new InvalidOperationException("No entry assembly");
            var alwaysIncludeTypes = new[]
            {
                typeof(IOrgFreedesktopDbusObjectManager)
            };
            var candidateTypes = alwaysIncludeTypes
                .Concat(entry.GetTypes())
                .Concat(entry.GetReferencedAssemblies()
                    .Select(x => Assembly.Load(x))
                    .SelectMany(x => x.GetTypes())
                )
            ;

            var shouldGenerateServiceCollectionExtension = candidateTypes.Where(
                x => x.FullName == "Microsoft.Extensions.DependencyInjection.IServiceCollection"
            ).Any();

            var registrations = new List<StringBuilder>();
            var services = new List<StringBuilder>();
            var result = new StringBuilder();

            registrations.Add(new StringBuilder()
                .Append(Indent)
                .AppendLine("global::Dbus.Connection.AddPublishProxy<global::Dbus.IOrgFreedesktopDbusObjectManagerProvide>(global::Dbus.OrgFreedesktopDbusObjectManager_Proxy.Factory);"))
            ;

            foreach (var type in candidateTypes.Distinct().OrderBy(x => x.FullName))
            {
                var dbusConsumeAttribute = type.GetCustomAttribute<DbusConsumeAttribute>();
                if (dbusConsumeAttribute != null)
                {
                    result.Append(consume(type, dbusConsumeAttribute));
                    registrations.Add(new StringBuilder()
                        .Append(Indent)
                        .Append("global::Dbus.Connection.AddConsumeImplementation<")
                        .Append(BuildTypeString(type))
                        .Append(">(")
                        .Append(type.Name)
                        .AppendLine("_Implementation.Factory);")
                    );
                    services.Add(BuildTypeString(type));
                }

                var dbusProvideAttribute = type.GetCustomAttribute<DbusProvideAttribute>();
                if (dbusProvideAttribute != null)
                {
                    result.Append(provide(type, dbusProvideAttribute));
                    registrations.Add(new StringBuilder()
                        .Append(Indent)
                        .Append("global::Dbus.Connection.AddPublishProxy<")
                        .Append(BuildTypeString(type))
                        .Append(">(")
                        .Append(type.Name)
                        .AppendLine("_Proxy.Factory);")
                    );
                }
            }

            var initClass = init(
                shouldGenerateServiceCollectionExtension,
                registrations,
                services,
                registerServices
            );
            return initClass.Append(result).ToString();
        }
    }
}
