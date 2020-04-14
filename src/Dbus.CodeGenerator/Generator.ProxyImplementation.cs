using System;
using System.Collections.Generic;
using System.Linq;
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

            var methodParameters = new List<string>();
            var decoded = new List<(string signature, string variable)>();
            foreach (var parameter in method.GetParameters())
            {
                if (parameter.ParameterType == typeof(CancellationToken))
                    methodParameters.Add("cancellationToken");
                else
                {
                    var decoder = DecoderGenerator.Create(method.Name + "_" + parameter.Name, parameter.ParameterType);
                    methodImplementation.AppendJoin(@"
", decoder.Delegates);
                    methodParameters.Add(parameter.Name!);
                    var variable = "var " + parameter.Name! + " = " + decoder.DelegateName + "(receivedMessage.Decoder);";
                    decoded.Add((decoder.Signature, variable));
                }
            }

            var encoder = new EncoderGenerator("sendBody");
            if (method.ReturnType != typeof(Task)) // Task<T>
            {
                var returnType = method.ReturnType.GenericTypeArguments[0];
                encoder.Add("methodResult", returnType, Indent);
            }

            methodImplementation.Append(@"
        private async global::System.Threading.Tasks.Task handle" + method.Name + @"(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.ReceivedMessage receivedMessage, global::System.Threading.CancellationToken cancellationToken)
        {
");
            methodImplementation.Append(Indent);
            methodImplementation.Append(@"receivedMessage.AssertSignature(""");
            methodImplementation.AppendJoin("", decoded.Select(x => x.signature));
            methodImplementation.AppendLine(@""");");
            foreach (var (_, variable) in decoded)
            {
                methodImplementation.Append(Indent);
                methodImplementation.AppendLine(variable);
            }
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
