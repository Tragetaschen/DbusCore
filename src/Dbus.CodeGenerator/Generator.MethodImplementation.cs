using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static string generateMethodImplementation(MethodInfo methodInfo, string interfaceName)
        {
            const string indent = "            ";

            if (!methodInfo.Name.EndsWith("Async"))
                throw new InvalidOperationException("Only method names ending with 'Async' are supported");
            var callName = methodInfo.Name.Substring(0, methodInfo.Name.Length - "Async".Length);

            var returnType = methodInfo.ReturnType;
            var returnTypeString = buildTypeString(returnType);
            if (returnTypeString != "System.Threading.Tasks.Task" &&
                !returnTypeString.StartsWith("System.Threading.Tasks.Task<"))
                throw new InvalidOperationException("Only Task based return types are supported");


            var encoder = new StringBuilder();
            var encoderSignature = string.Empty;
            var parameters = methodInfo.GetParameters();
            if (parameters.Length > 0)
            {
                encoder.Append(indent);
                encoder.AppendLine("var sendIndex = 0;");
                foreach (var parameter in parameters)
                {
                    encoder.Append(indent);
                    encoder.AppendLine("Encoder.Add(sendBody, ref sendIndex, " + parameter.Name + ");");
                    encoderSignature += signatures[parameter.ParameterType];
                }
            }

            var decoder = new StringBuilder();
            var decoderSignature = string.Empty;
            if (returnType == typeof(Task))
            {
                decoder.Append(indent);
                decoder.AppendLine("return;");
            }
            else // Task<T>
            {
                decoder.Append(indent);
                decoder.AppendLine("var index = 0;");
                var actualReturnType = returnType.GenericTypeArguments[0];
                if (!actualReturnType.IsConstructedGenericType)
                {
                    decoder.Append(indent);
                    decoder.AppendLine("var result = Decoder.Get" + actualReturnType.Name + "(receivedMessage.Body, ref index);");
                    decoder.Append(indent);
                    decoder.AppendLine("return result;");

                    decoderSignature += signatures[actualReturnType];
                }
                else
                {
                    var genericType = actualReturnType.GetGenericTypeDefinition();
                    if (genericType != typeof(IEnumerable<>))
                        throw new InvalidOperationException("Only IEnumerable is supported as generic type");
                    var elementType = actualReturnType.GenericTypeArguments[0];
                    decoder.Append(indent);
                    decoder.AppendLine("var result = Decoder.GetArray(receivedMessage.Body, ref index, Decoder.Get" + elementType.Name + ");");
                    decoder.Append(indent);
                    decoder.AppendLine("return result;");

                    decoderSignature += "a";
                    decoderSignature += signatures[elementType];
                }
            }

            return @"
        public async " + returnTypeString + @" " + methodInfo.Name + @"(" + string.Join(", ", methodInfo.GetParameters().Select(x => buildTypeString(x.ParameterType) + " " + x.Name)) + @")
        {
            var sendBody = Encoder.StartNew();
" + encoder + @"
            var receivedMessage = await connection.SendMethodCall(
                path,
                """ + interfaceName + @""",
                """ + callName + @""",
                destination,
                sendBody,
                """ + encoderSignature + @"""
            );
            assertSignature(receivedMessage.Signature, """ + decoderSignature + @""");
" + decoder + @"
        }
";
        }
    }
}
