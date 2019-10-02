﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        public const string Indent = "            ";

        public static string Run(Func<IEnumerable<string>, StringBuilder>? registerServices = null)
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

            var shouldGenerateServiceCollectionExtension = candidateTypes.Where(x => x.FullName == "Microsoft.Extensions.DependencyInjection.IServiceCollection").Any();

            var registrations = new List<string>();
            var services = new List<string>();
            var result = new StringBuilder();

            registrations.Add("global::Dbus.Connection.AddPublishProxy<global::Dbus.IOrgFreedesktopDbusObjectManagerProvide>(global::Dbus.OrgFreedesktopDbusObjectManager_Proxy.Factory);");

            foreach (var type in candidateTypes.Distinct().OrderBy(x => x.FullName))
            {
                var consume = type.GetTypeInfo().GetCustomAttribute<DbusConsumeAttribute>();
                if (consume != null)
                {
                    var (implementation, registration) = generateConsumeImplementation(type, consume);
                    result.Append(implementation);
                    registrations.Add(registration);
                    services.Add(BuildTypeString(type));
                }

                var provide = type.GetTypeInfo().GetCustomAttribute<DbusProvideAttribute>();
                if (provide != null)
                {
                    var (implementation, registration) = generateProvideImplementation(type, provide);
                    result.Append(implementation);
                    registrations.Add(registration);
                }
            }

            var initClass = new StringBuilder();
            initClass.Append(@"
    public static partial class DbusImplementations
    {
        private static void initRegistrations()
        {
            ");
            initClass.AppendJoin(@"
            ", registrations);
            initClass.Append(@"
        }
");

            if (registerServices != null)
                initClass.Append(registerServices(services));
            else if (!shouldGenerateServiceCollectionExtension)
                initClass.Append(@"
        static partial void DoInit() => initRegistrations();");
            else
                initClass.Append(@"
        static partial void DoAddDbus(global::Microsoft.Extensions.DependencyInjection.IServiceCollection services)
        {
            initRegistrations();
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
        }");
            initClass.Append(@"
    }
");
            return initClass.Append(result).ToString();
        }

        private static (string implementation, string registration) generateConsumeImplementation(Type type, DbusConsumeAttribute consume)
        {
            if (!typeof(IDisposable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidOperationException("The interface " + type.Name + " is meant to be consumed, but does not extend IDisposable");
            if (!typeof(IAsyncDisposable).GetTypeInfo().IsAssignableFrom(type))
                throw new InvalidOperationException("The interface " + type.Name + " is meant to be consumed, but does not extend IAsyncDisposable");

            var className = type.Name + "_Implementation";
            var eventSubscriptions = new StringBuilder();
            var methodImplementations = new StringBuilder();
            var eventImplementations = new StringBuilder();
            var propertyImplementations = new StringBuilder();
            var typeInfo = type.GetTypeInfo();

            foreach (var eventInfo in typeInfo.GetEvents().OrderBy(x => x.Name))
            {
                var (subscription, implementation) = GenerateEventImplementation(eventInfo, consume.InterfaceName);
                eventSubscriptions.Append(subscription);
                eventImplementations.Append(implementation);
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

                eventSubscriptions.Append(@"            eventSubscriptions.Add(connection.RegisterSignalHandler(
                this.path,
                ""org.freedesktop.DBus.Properties"",
                ""PropertiesChanged"",
                this.handleProperties
            ));
            PropertyInitializationFinished = global::System.Threading.Tasks.Task.Run(() => initProperties(cancellationToken), cancellationToken);
");
                propertyImplementations.Append(@"
        private void handleProperties(global::Dbus.ReceivedMessage receivedMessage)
        {
            receivedMessage.AssertSignature(""sa{sv}as"");
            var decoder = receivedMessage.Decoder;
            var interfaceName = decoder.GetString();
            if (interfaceName != """ + consume.InterfaceName + @""")
                return;
            var changed = decoder.GetDictionary(decoder.GetString, decoder.GetObject);
            applyProperties(changed);
        }

        private async global::System.Threading.Tasks.Task initProperties(global::System.Threading.CancellationToken cancellationToken)
        {
            var sendBody = new global::Dbus.Encoder();
            sendBody.Add(""" + consume.InterfaceName + @""");

            var receivedMessage = await connection.SendMethodCall(
                this.path,
                ""org.freedesktop.DBus.Properties"",
                ""GetAll"",
                this.destination,
                sendBody,
                ""s"",
                cancellationToken
            ).ConfigureAwait(false);
            using (receivedMessage)
            {
                receivedMessage.AssertSignature(""a{sv}"");
                var properties = receivedMessage.Decoder.GetDictionary(
                    receivedMessage.Decoder.GetString,
                    receivedMessage.Decoder.GetObject
                );
                applyProperties(properties);
            }
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
        private readonly global::System.Collections.Generic.List<global::System.IAsyncDisposable> eventSubscriptions = new global::System.Collections.Generic.List<global::System.IAsyncDisposable>();

        private " + className + @"(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            this.connection = connection;
            this.path = path ?? """ + consume.Path + @""";
            this.destination = destination ?? """ + consume.Destination + @""";
" + eventSubscriptions + @"        }

        public override string ToString()
        {
            return """ + consume.InterfaceName + @"@"" + this.path;
        }

        public static " + BuildTypeString(type) + @" Factory(global::Dbus.Connection connection, global::Dbus.ObjectPath path, string destination, global::System.Threading.CancellationToken cancellationToken)
        {
            return new " + className + @"(connection, path, destination, cancellationToken);
        }
" + propertyImplementations + methodImplementations + @"
" + eventImplementations + @"

        public void Dispose() => global::System.Threading.Tasks.Task.Run(DisposeAsync);

        public async global::System.Threading.Tasks.ValueTask DisposeAsync()
        {
            foreach (var eventSubscription in eventSubscriptions)
                await eventSubscription.DisposeAsync();
        }
    }
";
            return (implementationClass, registration);
        }

        private static (string implementation, string registration) generateProvideImplementation(Type type, DbusProvideAttribute provide)
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

                var (name, implementation) = GenerateMethodProxy(method);

                knownMethods.Add(name);
                proxies.Append(implementation);
            }

            var proxyRegistration = "global::Dbus.Connection.AddPublishProxy<" + BuildTypeString(type) + ">(" + type.Name + "_Proxy.Factory);";
            var proxyClass = new StringBuilder(@"
    public sealed class " + type.Name + @"_Proxy : global::Dbus.IProxy
    {

        public string InterfaceName { get; }

        private readonly global::Dbus.Connection connection;
        private readonly " + BuildTypeString(type) + @" target;
        private readonly global::Dbus.ObjectPath path;
        private readonly global::System.IDisposable registration;

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

            target.PropertyChanged += handlePropertyChangedEventAsync;");

            proxyClass.Append(@"
        }

        public object Target => target;

        public override string ToString()
        {
            return this.InterfaceName + ""@"" + this.path;
        }

        public static " + type.Name + @"_Proxy Factory(global::Dbus.Connection connection, " + type.FullName + @" target, global::Dbus.ObjectPath path)
        {
            return new " + type.Name + @"_Proxy(connection, target, path);
        }");
            if (typeof(System.ComponentModel.INotifyPropertyChanged).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                proxyClass.Append(@"
        private async void handlePropertyChangedEventAsync(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            var sendBody = new global::Dbus.Encoder();
            sendBody.Add(""" + provide.InterfaceName + @""");");
                //Only one property is changed at a time, so no foreach loop is necessary
                proxyClass.Append(@"
            sendBody.AddArray(() =>
            {
                sendBody.StartCompoundValue();
                sendBody.Add(e.PropertyName);
                switch (e.PropertyName)
                {");
                foreach (var property in type.GetTypeInfo().GetProperties())
                {
                    if (property.GetCustomAttribute<DbusPropertiesChanged>() != null)
                    {
                        proxyClass.Append(@"
                    case """ + property.Name + @""":
                        Encode" + property.Name + @"(sendBody);
                        break;
");
                    }
                }
                proxyClass.Append(@"
                    default:
                        throw new System.NotSupportedException(""Property encoding not supported for the given property"" + e.PropertyName);
                }
            }, storesCompoundValues: true);");
                //This is actually an empty array, but the encoding per 0-integer is more efficient
                proxyClass.Append(@"
            sendBody.Add(0);

            await connection.SendSignalAsync(
                path,
                ""org.freedesktop.DBus.Properties"",
                ""PropertiesChanged"",
                sendBody,
                ""sa{sv}as"",
                default(global::System.Threading.CancellationToken)
            ).ConfigureAwait(false);
        }");
            }
            proxyClass.Append(@"
"
            + generatePropertyEncodeImplementation(type) + @"

        public global::System.Threading.Tasks.Task HandleMethodCallAsync(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.ReceivedMessage receivedMessage, global::System.Threading.CancellationToken cancellationToken)
        {
            switch (methodCallOptions.Member)
            {
                " + string.Join(@"
                ", knownMethods.Select(x => @"case """ + x + @""":
                    return handle" + x + @"Async(methodCallOptions, receivedMessage, cancellationToken);")) + @"");
            proxyClass.Append(@"
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownMethod""),
                        ""Method not supported""
                    );
            }
        }
" + proxies.ToString() + @"

        public void Dispose()
        {
            registration.Dispose();
        }
    }
");
            return (proxyClass.ToString(), proxyRegistration);
        }

        public static string BuildTypeString(Type type)
        {
            if (!type.IsConstructedGenericType)
                return "global::" + type.FullName;

            var genericName = type.GetGenericTypeDefinition().FullName ?? throw new InvalidOperationException("Now full name");
            var withoutSuffix = genericName[0..^2];
            var result = "global::" + withoutSuffix + "<" +
                string.Join(", ", type.GenericTypeArguments.Select(BuildTypeString)) +
                ">"
            ;
            return result;
        }
    }
}
