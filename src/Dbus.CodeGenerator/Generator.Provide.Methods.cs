using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder provideMethods(MethodInfo[] methods)
        {
            var builder = new StringBuilder();

            foreach (var method in methods)
            {
                if (!method.Name.EndsWith("Async"))
                    throw new InvalidOperationException("Only method names ending in 'Async' are supported");
                if (method.ReturnType != typeof(Task) && method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                    throw new InvalidOperationException("Only methods returning a Task type are supported");

                builder.Append(provideMethodImplementation(method));
            }

            builder
                .Append(@"
        public global::System.Threading.Tasks.Task HandleMethodCallAsync(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.Decoder decoder, global::System.Threading.CancellationToken cancellationToken)
        {
            switch (methodCallOptions.Member)
            {")
                .AppendJoin(@"", methods.Select(method => new StringBuilder()
                    .Append(@"
                case """)
                    .Append(method.Name[0..^"Async".Length])
                    .Append(@""":
                    return handle")
                    .Append(method.Name)
                    .Append(@"(methodCallOptions, decoder, cancellationToken);")
                ))
                .AppendLine(@"
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownMethod""),
                        ""Method not supported""
                    );
            }
        }");

            return builder;
        }
    }
}
