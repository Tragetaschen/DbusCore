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

            var sendSignature = "";
            var encoders = new StringBuilder();
            if (method.ReturnType != typeof(Task)) // Task<T>
            {
                encoders.Append(Indent);
                encoders.AppendLine("var sendIndex = 0;");

                var returnType = method.ReturnType.GenericTypeArguments[0];
                if (returnType.FullName.StartsWith("System.Tuple"))
                {
                    var tupleParameters = returnType.GenericTypeArguments;
                    var counter = 0;
                    foreach (var p in tupleParameters)
                    {
                        ++counter;
                        sendSignature += SignatureString.For[p];
                        encoders.Append(Indent);
                        encoders.AppendLine("global::Dbus.Encoder.Add(sendBody, ref sendIndex, result.Item" + counter + ");");
                    }
                }
                else
                {
                    encoders.Append(Indent);
                    sendSignature += EncoderGenerator.BuildSignature(returnType, encoders);
                }
            }
            methodImplementation.Append(Indent);
            methodImplementation.AppendLine(@"assertSignature(header.BodySignature, """ + decoder.Signature + @""");");
            methodImplementation.Append(decoder.Result);
            methodImplementation.Append(Indent);
            if (sendSignature != "")
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
            if (sendSignature != "")
                methodImplementation.Append(encoders);
            methodImplementation.Append(Indent);
            methodImplementation.Append(@"await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody, """ + sendSignature + @""").ConfigureAwait(false);");
            methodImplementation.AppendLine(@"
            }");
            methodImplementation.AppendLine(@"
        }");

            return Tuple.Create(methodName, methodImplementation.ToString());
        }
    }
}
