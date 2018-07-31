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
        private global::System.Threading.Tasks.Task handle" + method.Name + @"(uint replySerial, global::Dbus.MessageHeader header, global::System.ReadOnlySpan<byte> receivedBody, bool shouldSendReply)
        {
");
            var decoder = new DecoderGenerator("receivedBody", "header");
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
            methodImplementation.AppendLine(@"header.BodySignature.AssertEqual(""" + decoder.Signature + @""");");
            methodImplementation.Append(decoder.Result);
            methodImplementation.Append(Indent);
            methodImplementation.Append("return target." + method.Name + "(");
            methodImplementation.Append(string.Join(", ", parameters.Select(x => x.Name)));
            methodImplementation.AppendLine(@")
                .ContinueWith(async x =>
                {");
            methodImplementation.Append(continueIndent);
            methodImplementation.AppendLine(@"if (!shouldSendReply)
                        return;");
            methodImplementation.Append(continueIndent);
            methodImplementation.AppendLine("var sendBody = global::Dbus.Encoder.StartNew();");
            if (encoder.Signature != "")
                methodImplementation.Append(encoder.Result);
            methodImplementation.Append(continueIndent);
            methodImplementation.Append(@"await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, """ + encoder.Signature + @""").ConfigureAwait(false);");
            methodImplementation.AppendLine(@"
                });
        }");

            return (methodName, methodImplementation.ToString());
        }
    }
}
