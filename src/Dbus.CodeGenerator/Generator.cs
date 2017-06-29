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
                    services.Add(BuildTypeString(type));
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
            global::Dbus.Connection.AddPublishProxy<Dbus.IOrgFreedesktopDbusObjectManagerProvide>(Dbus.OrgFreedesktopDbusObjectManager_Proxy.Factory);
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
                var connection = await Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetService<global::System.Threading.Tasks.Task<global::Dbus.Connection>>(serviceProvider).ConfigureAwait(false);
                return connection.Consume<" + x + @">();
            });")) + @"
        }
    }
";
            return initClass + result.ToString();
        }

        private static Tuple<string, string> generateConsumeImplementation(Type type, DbusConsumeAttribute consume)
        {
            if (!typeof(IDisposable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidOperationException("The interface " + type.Name + " is meant to be consumed, but does not extend IDisposable");

            var className = type.Name + "_Implementation";
            var eventSubscriptions = new StringBuilder();
            var methodImplementations = new StringBuilder();
            var eventImplementations = new StringBuilder();
            var propertyImplementations = new StringBuilder();
            var typeInfo = type.GetTypeInfo();

            foreach (var eventInfo in typeInfo.GetEvents().OrderBy(x => x.Name))
            {
                var result = generateEventImplementation(eventInfo, consume.InterfaceName);
                eventSubscriptions.Append(result.Item1);
                eventImplementations.Append(result.Item2);
            }
            foreach (var methodInfo in typeInfo.GetMethods().OrderBy(x => x.Name))
            {
                if (!methodInfo.IsSpecialName)
                    methodImplementations.Append(generateMethodImplementation(methodInfo, consume.InterfaceName));
            }

            var properties = typeInfo.GetProperties();
            if (properties.Length > 0)
            {
                if (!typeof(IDbusPropertyInitialization).GetTypeInfo().IsAssignableFrom(typeInfo))
                    throw new InvalidOperationException("Interface " + type.Name + " with cache properties does not implement 'IDbusPropertyInitialization'");
                if (!typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(typeInfo))
                    throw new InvalidOperationException("Interface " + type.Name + " with cache properties does not implement 'INotifyPropertyChanged'");

                eventSubscriptions.Append(@"
            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                ""org.freedesktop.DBus.Properties"",
                ""PropertiesChanged"",
                this.handleProperties
            ));
            PropertyInitializationFinished = global::System.Threading.Tasks.Task.Run(initProperties);
");
                propertyImplementations.Append(@"
        private void handleProperties(global::Dbus.MessageHeader header, byte[] body)
        {
            assertSignature(header.BodySignature, ""sa{sv}as"");
            var index = 0;
            var interfaceName = global::Dbus.Decoder.GetString(body, ref index);
            var changed = global::Dbus.Decoder.GetDictionary(body, ref index, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            //var invalidated = global::Dbus.Decoder.GetArray(body, ref index, global::Dbus.Decoder.GetString);
            applyProperties(changed);
        }

        private async global::System.Threading.Tasks.Task initProperties()
        {
            var sendBody = global::Dbus.Encoder.StartNew();
            var sendIndex = 0;
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, """ + consume.InterfaceName + @""");

            var receivedMessage = await connection.SendMethodCall(
                this.path,
                ""org.freedesktop.DBus.Properties"",
                ""GetAll"",
                this.destination,
                sendBody,
                ""s""
            ).ConfigureAwait(false);
            assertSignature(receivedMessage.Signature, ""a{sv}"");
            var index = 0;
            var properties = global::Dbus.Decoder.GetDictionary(receivedMessage.Body, ref index, global::Dbus.Decoder.GetString, global::Dbus.Decoder.GetObject);
            applyProperties(properties);
        }

        private void applyProperties(global::System.Collections.Generic.IDictionary<string, object> changed)
        {
            foreach (var entry in changed)
            {
                switch (entry.Key)
                {");
                foreach (var property in properties)
                {
                    if (property.SetMethod != null)
                        throw new InvalidOperationException("Cache properties can only have getters");
                    propertyImplementations.Append(@"
                    case """ + property.Name + @""":
                        " + property.Name + @" = (" + BuildTypeString(property.PropertyType) + @")entry.Value;
                        break;");
                }
                propertyImplementations.Append(@"
                }
                PropertyChanged?.Invoke(this, new global::System.ComponentModel.PropertyChangedEventArgs(entry.Key));
            }
        }

        public event global::System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        public global::System.Threading.Tasks.Task PropertyInitializationFinished { get; }
");
                foreach (var property in properties)
                    propertyImplementations.Append(@"
        public " + BuildTypeString(property.PropertyType) + " " + property.Name + " { get; private set; }");
            }

            var registration = "global::Dbus.Connection.AddConsumeImplementation<" + BuildTypeString(type) + ">(" + className + ".Factory);";
            var implementationClass = @"
    public sealed class " + className + @" : " + BuildTypeString(type) + @"
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

        public static " + BuildTypeString(type) + @" Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination)
        {
            return new " + className + @"(connection, path, destination);
        }

" + propertyImplementations + methodImplementations + @"
" + eventImplementations + @"
        private static void assertSignature(global::Dbus.Signature actual, global::Dbus.Signature expected)
        {
            if (actual != expected)
                throw new System.InvalidOperationException($""Unexpected signature. Got '{actual}', but expected '{expected}'"");
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

            var proxyRegistration = "global::Dbus.Connection.AddPublishProxy<" + BuildTypeString(type) + ">(" + type.Name + "_Proxy.Factory);";
            StringBuilder proxyClass = new StringBuilder(@"
    public sealed class " + type.Name + @"_Proxy : global::System.IDisposable, global::Dbus.IProxy
    {

        public string InterfaceName { get; }

        private readonly global::Dbus.Connection connection;
        private readonly " + BuildTypeString(type) + @" target;
        private readonly global::Dbus.ObjectPath path;

        private global::System.IDisposable registration;

        private " + type.Name + @"_Proxy(global::Dbus.Connection connection, " + BuildTypeString(type) + @" target, global::Dbus.ObjectPath path)
        {
            this.connection = connection;
            this.target = target;
            this.path = path;
            InterfaceName = """ + provide.InterfaceName + @""";
            registration = connection.RegisterObjectProxy(
                path ?? """ + provide.Path + @""",
                InterfaceName,
                this
            );");

            if (typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
                proxyClass.Append(@"

            target.PropertyChanged += HandlePropertyChangedEventAsync;");

            proxyClass.Append(@"
        }

        public static " + type.Name + @"_Proxy Factory(global::Dbus.Connection connection, " + type.FullName + @" target, global::Dbus.ObjectPath path)
        {
            return new " + type.Name + @"_Proxy(connection, target, path);
        }");
            if (typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                proxyClass.Append(@"
            private async void HandlePropertyChangedEventAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
            {
                var sendBody = global::Dbus.Encoder.StartNew();
                var sendIndex = 0;
                global::Dbus.Encoder.Add(sendBody, ref sendIndex, """ + provide.InterfaceName + @""");");
                //Only one property is changed at a time, so no foreach loop is necessary
                proxyClass.Append(@"
                global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e, ref int sendIndex_e) =>
                {
                        global::Dbus.Encoder.EnsureAlignment(sendBody_e, ref sendIndex_e, 8);
                        global::Dbus.Encoder.Add(sendBody_e, ref sendIndex_e, e.PropertyName);
                        switch(e.PropertyName)
                        {");
                foreach (var property in type.GetTypeInfo().GetProperties())
                {
                    if (property.GetCustomAttribute<DbusPropertiesChanged>() != null)
                    {
                        proxyClass.Append(@"
                            case """ + property.Name + @""":
                                Encode" + property.Name + @"(sendBody_e, ref sendIndex_e);
                                break;
");
                    }
                }
                proxyClass.Append(@"
                            default:
                                throw new System.NotSupportedException(""Property encoding not supported for the given property"" + e.PropertyName);
                        }
                }, true);");
                //This is actually an empty array, but the encoding per 0-integer is more efficient
                proxyClass.Append(@"
                global::Dbus.Encoder.Add(sendBody, ref sendIndex, 0);

                    await connection.SendSignal(
                    path,
                    ""org.freedesktop.DBus.Properties"",
                    ""PropertiesChanged"",
                    sendBody,
                    ""sa{sv}as""
                );
            }");
            }
            proxyClass.Append(@"
"
            + generatePropertyEncodeImplementation(type) + @"

            public System.Threading.Tasks.Task HandleMethodCallAsync(uint replySerial, global::Dbus.MessageHeader header, byte[] body, bool shouldSendReply)
        {
            switch (header.Member)
            {
                " + string.Join(@"
                ", knownMethods.Select(x => @"case """ + x + @""":
                    return handle" + x + @"Async(replySerial, header, body, shouldSendReply);")) + @"");
            proxyClass.Append(@"
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
");
            return Tuple.Create(proxyClass.ToString(), proxyRegistration);
        }

        public static string BuildTypeString(Type type)
        {
            if (!type.IsConstructedGenericType)
                return "global::" + type.FullName;

            var genericName = type.GetGenericTypeDefinition().FullName;
            var withoutSuffix = genericName.Substring(0, genericName.Length - 2);
            var result = "global::" + withoutSuffix + "<" +
                string.Join(", ", type.GenericTypeArguments.Select(BuildTypeString)) +
                ">"
            ;
            return result;
        }
    }
}
