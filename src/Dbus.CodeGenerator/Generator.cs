using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static Dictionary<Type, string> signatures = new Dictionary<Type, string>()
        {
            [typeof(ObjectPath)] = "o",
            [typeof(string)] = "s",
            [typeof(Signature)] = "g",
            [typeof(byte)] = "y",
            [typeof(bool)] = "b",
            [typeof(int)] = "i",
            [typeof(uint)] = "u",
            [typeof(object)] = "v",
            [typeof(long)] = "x",
        };
        const string indent = "            ";

        public static string Run()
        {
            var entry = Assembly.GetEntryAssembly();
            var candidateTypes = entry
                .GetTypes()
                .Concat(entry.GetReferencedAssemblies()
                    .Select(x => Assembly.Load(x))
                    .SelectMany(x => x.GetTypes())
                )
            ;

            var shouldGenerateServiceCollectionExtension = candidateTypes.Where(x => x.FullName == "Microsoft.Extensions.DependencyInjection.IServiceCollection").Any();

            var registrations = new List<string>();
            var services = new List<string>();
            var result = new StringBuilder();

            foreach (var type in candidateTypes.OrderBy(x => x.FullName))
            {
                var consume = type.GetTypeInfo().GetCustomAttribute<DbusConsumeAttribute>();
                if (consume != null)
                {
                    var consumeImplementation = generateConsumeImplementation(type, consume);
                    result.Append(consumeImplementation.Item1);
                    registrations.Add(consumeImplementation.Item2);
                    services.Add(buildTypeString(type));
                }

                var provide = type.GetTypeInfo().GetCustomAttribute<DbusProvideAttribute>();
                if (provide != null)
                {
                    var provideImplementation = generateProvideImplementation(type, provide);
                    result.Append(provideImplementation.Item1);
                    registrations.Add(provideImplementation.Item2);
                }
            }

            var initClass = "";
            if (!shouldGenerateServiceCollectionExtension)
                initClass = @"
    public static partial class DbusImplementations
    {
        static partial void DoInit()
        {
            " + string.Join(@"
            ", registrations) + @"
        }
    }
";
            else
                initClass = @"
    public static partial class DbusImplementations
    {
        static partial void DoAddDbus(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            " + string.Join(@"
            ", registrations) + @"
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, serviceProvider =>
            {
                var options = global::Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::Microsoft.Extensions.Options.IOptions<global::Dbus.DbusConnectionOptions>>(serviceProvider);
                return global::Dbus.Connection.CreateAsync(options.Value);
            });" + string.Join("", services.Select(x => @"
            global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton(services, async serviceProvider =>
            {
                var connection = await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::System.Threading.Tasks.Task<global::Dbus.Connection>>(serviceProvider);
                return connection.Consume<" + x + @">();
            });")) + @"
        }
    }
";

            return initClass + result.ToString();
        }

        private static Tuple<string, string> generateConsumeImplementation(Type type, DbusConsumeAttribute consume)
        {
            var className = type.Name.Substring(1);
            var eventSubscriptions = new StringBuilder();
            var methodImplementations = new StringBuilder();
            var eventImplementations = new StringBuilder();

            var members = type.GetTypeInfo().GetMembers();
            foreach (var member in members.OrderBy(x => x.Name))
            {
                MethodInfo methodInfo;
                EventInfo eventInfo;

                if ((eventInfo = member as EventInfo) != null)
                {
                    var result = generateEventImplementation(eventInfo, consume.InterfaceName);
                    eventSubscriptions.Append(result.Item1);
                    eventImplementations.Append(result.Item2);
                }
                else if ((methodInfo = member as MethodInfo) != null)
                {
                    if (!methodInfo.IsSpecialName)
                        methodImplementations.Append(generateMethodImplementation(methodInfo, consume.InterfaceName));
                }
            }

            var registration = "global::Dbus.Connection.AddConsumeImplementation<" + buildTypeString(type) + ">(" + className + ".Factory);";
            var implementationClass = @"
    public sealed class " + className + @" : " + buildTypeString(type) + @"
    {
        private readonly global::Dbus.Connection connection;
        private readonly global::Dbus.ObjectPath path;
        private readonly string destination;
        private readonly global::System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new global::System.Collections.Generic.List<System.IDisposable>();

        private " + className + @"(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            this.connection = connection;
            this.path = path ?? """ + consume.Path + @""";
            this.destination = destination ?? """ + consume.Destination + @""";
" + eventSubscriptions + @"
        }

        public static " + buildTypeString(type) + @" Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            return new " + className + @"(connection, path, destination);
        }

" + methodImplementations + @"
" + eventImplementations + @"
        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($""Unexpected signature. Got ${ actual}, but expected ${ expected}"");
        }

        public void Dispose()
        {
            eventSubscriptions.ForEach(x => x.Dispose());
        }
    }
";
            return Tuple.Create(implementationClass, registration);
        }

        private static Tuple<string, string> generateProvideImplementation(Type type, DbusProvideAttribute provide)
        {
            var knownMethods = new List<string>();
            var proxies = new StringBuilder();

            var methods = type.GetTypeInfo().GetMethods();
            foreach (var method in methods.OrderBy(x => x.Name))
            {
                if (method.IsSpecialName)
                    continue;
                if (method.DeclaringType == typeof(object))
                    continue;

                var result = generateMethodProxy(method);

                knownMethods.Add(result.Item1);
                proxies.Append(result.Item2);
            }

            var proxyRegistration = "global::Dbus.Connection.AddPublishProxy<" + buildTypeString(type) + ">(" + type.Name + "_Proxy.Factory);";
            var proxyClass = @"
    public sealed class " + type.Name + @"_Proxy: global::System.IDisposable
    {
        private readonly global::Dbus.Connection connection;
        private readonly " + buildTypeString(type) + @" target;

        private global::System.IDisposable registration;

        private " + type.Name + @"_Proxy(global::Dbus.Connection connection, " + buildTypeString(type) + @" target, global::Dbus.ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            registration = connection.RegisterObjectProxy(
                path ?? """ + provide.Path + @""",
                """ + provide.InterfaceName + @""",
                handleMethodCall
            );
        }

        public static " + type.Name + @"_Proxy Factory(global::Dbus.Connection connection, " + type.FullName + @" target, global::Dbus.ObjectPath path)
        {
            return new " + type.Name + @"_Proxy(connection, target, path);
        }

        private System.Threading.Tasks.Task handleMethodCall(uint replySerial, global::Dbus.MessageHeader header, byte[] body)
        {
            switch (header.Member)
            {
                " + string.Join(@"
                ", knownMethods.Select(x => @"case """ + x + @""":
                    return handle" + x + @"Async(replySerial, header, body);")) + @"
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownMethod""),
                        ""Method not supported""
                    );
            }
        }
" + proxies.ToString() + @"

        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new global::Dbus.DbusException(
                    global::Dbus.DbusException.CreateErrorName(""InvalidSignature""),
                    ""Invalid signature""
                );
        }

        public void Dispose()
        {
            registration.Dispose();
        }
    }
";

            return Tuple.Create(proxyClass, proxyRegistration);
        }

        private static string buildTypeString(Type type)
        {
            if (!type.IsConstructedGenericType)
                return "global::" + type.FullName;

            var genericName = type.GetGenericTypeDefinition().FullName;
            var withoutSuffix = genericName.Substring(0, genericName.Length - 2);
            var result = "global::" + withoutSuffix + "<" +
                string.Join(",", type.GenericTypeArguments.Select(buildTypeString)) +
                ">"
            ;
            return result;
        }
    }
}
