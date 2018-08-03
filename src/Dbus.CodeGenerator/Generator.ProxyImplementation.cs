using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        public static (string name, string implementation) GenerateMethodProxy(MethodInfo method)
        {
            if (!method.Name.EndsWith("Async"))
                throw new InvalidOperationException("Only method names ending in 'Async' are supported");
            if (method.ReturnType != typeof(Task) && method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                throw new InvalidOperationException("Only methods returning a Task type are supported");

            var methodName = method.Name.Substring(0, method.Name.Length - "Async".Length);
            var methodImplementation = new StringBuilder();

            methodImplementation.Append(@"
        private global::System.Threading.Tasks.Task handle" + method.Name + @"(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.ReceivedMessage receivedMessage)
        {
");
            var decoder = new DecoderGenerator("receivedMessage.Decoder", "receivedMessage");
            var parameters = method.GetParameters();
            foreach (var parameter in method.GetParameters())
                decoder.Add(parameter.Name, parameter.ParameterType);

            var continueIndent = Indent + "        ";
            var encoder = new EncoderGenerator("sendBody");
            if (method.ReturnType != typeof(Task)) // Task<T>
            {
                var returnType = method.ReturnType.GenericTypeArguments[0];
                encoder.Add("x.Result", returnType, continueIndent);
            }

            methodImplementation.Append(Indent);
            methodImplementation.AppendLine(@"receivedMessage.AssertSignature(""" + decoder.Signature + @""");");
            methodImplementation.Append(decoder.Result);
            methodImplementation.Append(Indent);
            methodImplementation.Append("return target." + method.Name + "(");
            methodImplementation.Append(string.Join(", ", parameters.Select(x => x.Name)));
            methodImplementation.AppendLine(@")
                .ContinueWith(async x =>
                {");
            methodImplementation.Append(continueIndent);
            methodImplementation.AppendLine(@"if (!methodCallOptions.ShouldSendReply)
                        return;");
            methodImplementation.Append(continueIndent);
            methodImplementation.AppendLine("var sendBody = new global::Dbus.Encoder();");
            if (encoder.Signature != "")
                methodImplementation.Append(encoder.Result);
            methodImplementation.Append(continueIndent);
            methodImplementation.Append(@"await connection.SendMethodReturnAsync(methodCallOptions, sendBody, """ + encoder.Signature + @""").ConfigureAwait(false);");
            methodImplementation.AppendLine(@"
                });
        }");

            return (methodName, methodImplementation.ToString());
        }
    }
}
