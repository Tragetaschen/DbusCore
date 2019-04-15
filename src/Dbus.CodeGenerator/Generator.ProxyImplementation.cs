using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
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
        private async global::System.Threading.Tasks.Task handle" + method.Name + @"(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.ReceivedMessage receivedMessage, global::System.Threading.CancellationToken cancellationToken)
        {
");
            var decoder = new DecoderGenerator("receivedMessage.Decoder", "receivedMessage");
            var methodParameters = new List<string>();
            foreach (var parameter in method.GetParameters())
            {
                if (parameter.ParameterType == typeof(CancellationToken))
                    methodParameters.Add("cancellationToken");
                else
                {
                    decoder.Add(parameter.Name, parameter.ParameterType);
                    methodParameters.Add(parameter.Name);
                }
            }

            var encoder = new EncoderGenerator("sendBody");
            if (method.ReturnType != typeof(Task)) // Task<T>
            {
                var returnType = method.ReturnType.GenericTypeArguments[0];
                encoder.Add("methodResult", returnType, Indent);
            }

            methodImplementation.Append(Indent);
            methodImplementation.AppendLine(@"receivedMessage.AssertSignature(""" + decoder.Signature + @""");");
            methodImplementation.Append(decoder.Result);
            methodImplementation.Append(Indent);
            if (encoder.Signature != "")
                methodImplementation.Append("var methodResult = ");
            methodImplementation.Append("await target." + method.Name + "(");
            methodImplementation.Append(string.Join(", ", methodParameters));
            methodImplementation.AppendLine(");");
            methodImplementation.Append(Indent);
            methodImplementation.AppendLine("if (!methodCallOptions.ShouldSendReply)");
            methodImplementation.Append(Indent);
            methodImplementation.AppendLine("    return;");
            methodImplementation.Append(Indent);
            methodImplementation.AppendLine("var sendBody = new global::Dbus.Encoder();");
            if (encoder.Signature != "")
                methodImplementation.Append(encoder.Result);
            methodImplementation.Append(Indent);
            methodImplementation.AppendLine(@"await connection.SendMethodReturnAsync(methodCallOptions, sendBody, """ + encoder.Signature + @""", cancellationToken).ConfigureAwait(false);");
            methodImplementation.Append(@"
        }");

            return (methodName, methodImplementation.ToString());
        }
    }
}
