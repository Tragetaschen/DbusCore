using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static Regex propertyName = new Regex("^(Get|Set)[A-Z].+");

        private static string generateMethodImplementation(MethodInfo methodInfo, string interfaceName)
        {
            if (!methodInfo.Name.EndsWith("Async"))
                throw new InvalidOperationException("Only method names ending with 'Async' are supported");
            var callName = methodInfo.Name.Substring(0, methodInfo.Name.Length - "Async".Length);

            var returnType = methodInfo.ReturnType;
            var returnTypeString = buildTypeString(returnType);
            if (returnTypeString != "global::System.Threading.Tasks.Task" &&
                !returnTypeString.StartsWith("global::System.Threading.Tasks.Task<"))
                throw new InvalidOperationException("Only Task based return types are supported");

            var isProperty = propertyName.IsMatch(callName);

            var encoder = new StringBuilder();
            var encoderSignature = string.Empty;
            var parameters = methodInfo.GetParameters();

            if (isProperty)
                if (callName.StartsWith("Get"))
                    isProperty &= parameters.Length == 0;
                else if (callName.StartsWith("Set"))
                    isProperty &= parameters.Length == 1;

            if (parameters.Length > 0 || isProperty)
            {
                encoder.Append(indent);
                encoder.AppendLine("var sendIndex = 0;");
                if (isProperty)
                {
                    encoder.Append(indent);
                    encoder.AppendLine(@"global::Dbus.Encoder.Add(sendBody, ref sendIndex, """ + interfaceName + @""");");
                    encoder.Append(indent);
                    encoder.AppendLine(@"global::Dbus.Encoder.Add(sendBody, ref sendIndex, """ + callName.Substring(3 /* "Get" or "Set" */) + @""");");
                    encoderSignature += "ss";
                    interfaceName = "org.freedesktop.DBus.Properties";
                    callName = callName.Substring(0, 3); // "Get" or "Set"
                }
                foreach (var parameter in parameters)
                {
                    encoder.Append(indent);
                    encoder.AppendLine("global::Dbus.Encoder.Add(sendBody, ref sendIndex, " + parameter.Name + ");");
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
                if (isProperty) // must be "Get"
                {
                    decoder.Append(indent);
                    decoder.AppendLine("var result = (" + buildTypeString(actualReturnType) + ")global::Dbus.Decoder.GetObject(receivedMessage.Body, ref index);");
                    decoder.Append(indent);
                    decoder.AppendLine("return result;");

                    decoderSignature += signatures[typeof(object)];
                }
                else if (!actualReturnType.IsConstructedGenericType)
                {
                    decoder.Append(indent);
                    decoder.AppendLine("var result = global::Dbus.Decoder.Get" + actualReturnType.Name + "(receivedMessage.Body, ref index);");
                    decoder.Append(indent);
                    decoder.AppendLine("return result;");

                    decoderSignature += signatures[actualReturnType];
                }
                else
                {
                    var genericType = actualReturnType.GetGenericTypeDefinition();
                    if (genericType == typeof(IEnumerable<>))
                    {
                        var elementType = actualReturnType.GenericTypeArguments[0];
                        decoder.Append(indent);
                        decoder.AppendLine("var result = global::Dbus.Decoder.GetArray(receivedMessage.Body, ref index, global::Dbus.Decoder.Get" + elementType.Name + ");");
                        decoder.Append(indent);
                        decoder.AppendLine("return result;");

                        decoderSignature += "a";
                        decoderSignature += signatures[elementType];
                    }
                    else if (genericType == typeof(IDictionary<,>))
                    {
                        var keyType = actualReturnType.GenericTypeArguments[0];
                        var valueType = actualReturnType.GenericTypeArguments[1];

                        decoder.Append(indent);
                        decoder.Append("var result = global::Dbus.Decoder.GetDictionary(receivedMessage.Body, ref index");
                        decoder.Append(", global::Dbus.Decoder.Get" + keyType.Name);
                        decoder.Append(", global::Dbus.Decoder.Get" + valueType.Name);
                        decoder.AppendLine(");");
                        decoder.Append(indent);
                        decoder.AppendLine("return result;");

                        decoderSignature += "a{";
                        decoderSignature += signatures[keyType];
                        decoderSignature += signatures[valueType];
                        decoderSignature += "}";
                    }
                    else
                        throw new InvalidOperationException("Only IEnumerable and IDictionary are supported as generic type");
                }
            }

            return @"
        public async " + returnTypeString + @" " + methodInfo.Name + @"(" + string.Join(", ", methodInfo.GetParameters().Select(x => buildTypeString(x.ParameterType) + " " + x.Name)) + @")
        {
            var sendBody = global::Dbus.Encoder.StartNew();
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
