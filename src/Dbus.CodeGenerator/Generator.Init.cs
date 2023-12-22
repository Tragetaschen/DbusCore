using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dbus.CodeGenerator;

public static partial class Generator
{
    private static StringBuilder init(
        bool shouldGenerateServiceCollectionExtension,
        List<StringBuilder> registrations,
        List<StringBuilder> services,
        Func<List<StringBuilder>, StringBuilder>? registerServices
    )
    {
        var builder = new StringBuilder();
        builder
            .Append(@"
    public static partial class DbusImplementations
    {
        private static void initRegistrations()
        {
")
            .AppendJoin(@"", registrations)
            .AppendLine(@"        }")
        ;

        if (registerServices != null)
            builder.Append(registerServices(services));
        else if (!shouldGenerateServiceCollectionExtension)
            builder.Append(@"
        static partial void DoInit() => initRegistrations();");
        else
            builder
                .Append(@"
        static partial void DoAddDbus(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            initRegistrations();
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, serviceProvider =>
            {
                var options = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::Microsoft.Extensions.Options.IOptions<global::Dbus.DbusConnectionOptions>>(serviceProvider);
                return global::Dbus.Connection.CreateAsync(options.Value);
            });")
                .AppendJoin("", services.Select(service => new StringBuilder()
                    .Append(@"
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, async serviceProvider =>
            {
                var connection = await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::System.Threading.Tasks.Task<global::Dbus.Connection>>(serviceProvider).ConfigureAwait(false);
                return connection.Consume<")
                    .Append(service)
                    .Append(@">();
            });")))
                .Append(@"
        }");
        builder.AppendLine(@"
    }");

        return builder;
    }
}
