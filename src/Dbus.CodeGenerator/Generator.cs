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

            var proxyRegistrations = new List<string>();
            var result = new StringBuilder();

            foreach (var type in candidateTypes.OrderBy(x => x.FullName))
            {
                var consume = type.GetTypeInfo().GetCustomAttribute<DbusConsumeAttribute>();
                if (consume != null)
                    result.Append(generateConsumeImplementation(type, consume));

                var provide = type.GetTypeInfo().GetCustomAttribute<DbusProvideAttribute>();
                if (provide != null)
                {
                    var provideImplementation = generateProvideImplementation(type, provide);
                    result.Append(provideImplementation.Item1);
                    proxyRegistrations.Add(provideImplementation.Item2);
                }
            }

            var initClass = @"
    public static partial class DbusImplementations
    {
        static partial void DoInit()
        {
            " + string.Join(@"
            ", proxyRegistrations) + @"
        }
    }
";

            return initClass + result.ToString();
        }

        private static string generateConsumeImplementation(Type type, DbusConsumeAttribute consume)
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

            return @"
    public sealed class " + className + @" : " + type.FullName + @"
    {
        private readonly Connection connection;
        private readonly ObjectPath path;
        private readonly string destination;
        private readonly System.Collections.Generic.List<System.IDisposable> eventSubscriptions = new System.Collections.Generic.List<System.IDisposable>();

        public " + className + @"(Connection connection, ObjectPath path = null, string destination = null)
        {
            this.connection = connection;
            this.path = path ?? """ + consume.Path + @""";
            this.destination = destination ?? """ + consume.Destination + @""";
" + eventSubscriptions + @"
        }
" + methodImplementations + @"
" + eventImplementations + @"
        private static void assertSignature(Signature actual, Signature expected)
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

            var proxyRegistration = "Dbus.Connection.AddPublishProxy<" + type.Name + ">(" + type.Name + "_Proxy.Factory);";
            var proxyClass = @"
    public sealed class " + type.Name + @"_Proxy: System.IDisposable
    {
        private readonly Dbus.Connection connection;
        private readonly " + type.FullName + @" target;

        private System.IDisposable registration;

        private " + type.Name + @"_Proxy(Dbus.Connection connection, " + type.FullName + @" target, Dbus.ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            registration = connection.RegisterObjectProxy(
                path ?? """ + provide.Path + @""",
                """ + provide.InterfaceName + @""",
                handleMethodCall
            );
        }

        public static " + type.Name + @"_Proxy Factory(Dbus.Connection connection, " + type.FullName + @" target, Dbus.ObjectPath path)
        {
            return new " + type.Name + @"_Proxy(connection, target, path);
        }

        private System.Threading.Tasks.Task handleMethodCall(uint replySerial, Dbus.MessageHeader header, byte[] body)
        {
            switch (header.Member)
            {
                " + string.Join(@"
                ", knownMethods.Select(x => @"case """ + x + @""":
                    return handle" + x + @"Async(replySerial, header, body);")) + @"
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName(""UnknownMethod""),
                        ""Method not supported""
                    );
            }
        }
" + proxies.ToString() + @"

        private static void assertSignature(Signature actual, Signature expected)
        {
            if (actual != expected)
                throw new DbusException(
                    DbusException.CreateErrorName(""InvalidSignature""),
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
                return type.FullName;

            var genericName = type.GetGenericTypeDefinition().FullName;
            var withoutSuffix = genericName.Substring(0, genericName.Length - 2);
            var result = withoutSuffix + "<" +
                string.Join(",", type.GenericTypeArguments.Select(buildTypeString)) +
                ">"
            ;
            return result;
        }
    }
}
