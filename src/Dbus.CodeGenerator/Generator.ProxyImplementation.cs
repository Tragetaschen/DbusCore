using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        public static Tuple<string, string> generateMethodProxy(MethodInfo method)
        {
            if (!method.Name.EndsWith("Async"))
                throw new InvalidOperationException("Only method names ending in 'Async' are supported");
            if (method.ReturnType != typeof(Task) && method.ReturnType.GetGenericTypeDefinition() != typeof(Task<>))
                throw new InvalidOperationException("Only methods returning a Tak type are supported");

            var methodName = method.Name.Substring(0, method.Name.Length - "Async".Length);
            var methodImplementation = new StringBuilder();

            methodImplementation.Append(@"
        private async System.Threading.Tasks.Task handle" + method.Name + @"(uint replySerial, Dbus.MessageHeader header, byte[] receivedBody)
        {
");
            var decoders = new StringBuilder();
            var receivedSignature = "";
            var parameters = method.GetParameters();
            if (parameters.Length > 0)
            {
                decoders.Append(indent);
                decoders.AppendLine("var receiveIndex = 0;");

                foreach (var parameter in method.GetParameters())
                {
                    receivedSignature += signatures[parameter.ParameterType];
                    decoders.Append(indent);
                    decoders.AppendLine("var " + parameter.Name + " = Dbus.Decoder.Get" + parameter.ParameterType.Name + "(receivedBody, ref receiveIndex);");
                }
            }

            var sendSignature = "";
            var encoders = new StringBuilder();
            if (method.ReturnType != typeof(Task)) // Task<T>
            {
                encoders.Append(indent);
                encoders.AppendLine("var sendIndex = 0;");

                var returnType = method.ReturnType.GenericTypeArguments[0];
                if (returnType.FullName.StartsWith("System.Tuple"))
                {
                    var tupleParameters = returnType.GenericTypeArguments;
                    var counter = 0;
                    foreach (var p in tupleParameters)
                    {
                        ++counter;
                        sendSignature += signatures[p];
                        encoders.Append(indent);
                        encoders.AppendLine("Dbus.Encoder.Add(sendBody, ref sendIndex, result.Item" + counter + ");");
                    }
                }
                else
                {
                    sendSignature += signatures[returnType];
                    encoders.Append(indent);
                    encoders.AppendLine("Dbus.Encoder.Add(sendBody, ref sendIndex, result);");
                }
            }

            methodImplementation.Append(indent);
            methodImplementation.AppendLine(@"assertSignature(header.BodySignature, """ + receivedSignature + @""");");
            methodImplementation.Append(decoders);
            methodImplementation.Append(indent);
            if (sendSignature != "")
                methodImplementation.Append("var result = ");
            methodImplementation.Append("await target." + method.Name + "(");
            methodImplementation.Append(string.Join(", ", parameters.Select(x => x.Name)));
            methodImplementation.AppendLine(");");
            methodImplementation.Append(indent);
            methodImplementation.AppendLine("var sendBody = Dbus.Encoder.StartNew();");
            if (sendSignature != "")
                methodImplementation.Append(encoders);
            methodImplementation.Append(indent);
            methodImplementation.Append(@"await connection.SendMethodReturnAsync(replySerial, header.Sender, sendBody,""" + sendSignature + @""");");
            methodImplementation.AppendLine(@"
        }");

            return Tuple.Create(methodName, methodImplementation.ToString());
        }
    }
}
