using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        public static Tuple<string, string> GenerateMethodProxy(MethodInfo method)
        {
            if (!method.Name.EndsWith("Async"))
                throw new InvalidOperationException("Only method names ending in 'Async' are supported");
            if (method.ReturnType != typeof(Task) && method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                throw new InvalidOperationException("Only methods returning a Task type are supported");

            var methodName = method.Name.Substring(0, method.Name.Length - "Async".Length);
            var methodImplementation = new StringBuilder();

            methodImplementation.Append(@"
        private async System.Threading.Tasks.Task handle" + method.Name + @"(uint replySerial, global::Dbus.MessageHeader header, byte[] receivedBody, bool shouldSendReply)
        {
");
            var decoder = new DecoderGenerator("receivedBody", "header");
            var parameters = method.GetParameters();
            foreach (var parameter in method.GetParameters())
                decoder.Add(parameter.Name, parameter.ParameterType);

            var encoder = new EncoderGenerator("sendBody");
            if (method.ReturnType != typeof(Task)) // Task<T>
            {
                var returnType = method.ReturnType.GenericTypeArguments[0];
                encoder.Add("result", returnType);
            }

            methodImplementation.Append(Indent);
            methodImplementation.AppendLine(@"header.BodySignature.AssertEqual(""" + decoder.Signature + @""");");
            methodImplementation.Append(decoder.Result);
            methodImplementation.Append(Indent);
            if (encoder.Signature != "")
                methodImplementation.Append("var result = ");
            methodImplementation.Append("await target." + method.Name + "(");
            methodImplementation.Append(string.Join(", ", parameters.Select(x => x.Name)));
            methodImplementation.AppendLine(@").ConfigureAwait(false);
");
            methodImplementation.Append(Indent);
            methodImplementation.AppendLine(@"if (shouldSendReply)
            {");
            methodImplementation.Append(Indent);
            methodImplementation.AppendLine("var sendBody = global::Dbus.Encoder.StartNew();");
            if (encoder.Signature != "")
                methodImplementation.Append(encoder.Result);
            methodImplementation.Append(Indent);
            methodImplementation.Append(@"await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, """ + encoder.Signature + @""").ConfigureAwait(false);");
            methodImplementation.AppendLine(@"
            }");
            methodImplementation.AppendLine(@"
        }");

            return Tuple.Create(methodName, methodImplementation.ToString());
        }
    }
}
