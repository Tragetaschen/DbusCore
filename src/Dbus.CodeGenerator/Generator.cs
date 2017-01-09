using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

            var result = new StringBuilder();

            foreach (var type in candidateTypes)
            {
                var consume = type.GetTypeInfo().GetCustomAttribute<DbusConsumeAttribute>();

                if (consume != null)
                {
                    result.Append(generateConsumeImplementation(type, consume));
                }
            }

            return result.ToString();
        }

        private static string generateConsumeImplementation(Type type, DbusConsumeAttribute consume)
        {
            var className = type.Name.Substring(1);
            var eventSubscriptions = new StringBuilder();
            var methodImplementations = new StringBuilder();
            var eventImplementations = new StringBuilder();

            var members = type.GetTypeInfo().GetMembers();
            foreach (var member in members)
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

        public " + className + @"(Connection connection, ObjectPath path = null, string destination = """")
        {
            this.connection = connection;
            this.path = path == null ? """ + consume.Path + @""" : path;
            this.destination = destination == """" ? """ + consume.Destination + @""" : destination;
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
