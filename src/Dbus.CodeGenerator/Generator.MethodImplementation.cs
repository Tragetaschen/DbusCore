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
                throw new InvalidOperationException($"The method '{methodInfo.Name}' does not end with 'Async'");
            var callName = methodInfo.Name.Substring(0, methodInfo.Name.Length - "Async".Length);

            var returnType = methodInfo.ReturnType;
            var returnTypeString = BuildTypeString(returnType);
            if (returnTypeString != "global::System.Threading.Tasks.Task" &&
                !returnTypeString.StartsWith("global::System.Threading.Tasks.Task<"))
                throw new InvalidOperationException($"The method '{methodInfo.Name}' does not return a Task type");

            var isProperty = propertyName.IsMatch(callName);
            isProperty &= methodInfo.GetCustomAttribute<DbusMethodAttribute>() == null;

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
                encoder.Append(Indent);
                encoder.AppendLine("var sendIndex = 0;");
                if (isProperty)
                {
                    encoder.Append(Indent);
                    encoder.AppendLine(@"global::Dbus.Encoder.Add(sendBody, ref sendIndex, """ + interfaceName + @""");");
                    encoder.Append(Indent);
                    encoder.AppendLine(@"global::Dbus.Encoder.Add(sendBody, ref sendIndex, """ + callName.Substring(3 /* "Get" or "Set" */) + @""");");
                    encoderSignature += "ss";
                    interfaceName = "org.freedesktop.DBus.Properties";
                    callName = callName.Substring(0, 3); // "Get" or "Set"
                    foreach (var parameter in parameters)
                    {
                        encoder.Append(Indent);
                        encoder.AppendLine(@"global::Dbus.Encoder.Add(sendBody, ref sendIndex, (global::Dbus.Signature)""" + SignatureString.For[parameter.ParameterType] + @""");");
                        encoder.Append(Indent);
                        encoder.AppendLine("global::Dbus.Encoder.Add(sendBody, ref sendIndex, " + parameter.Name + ");");
                        encoderSignature += "v";
                    }
                }
                else
                    foreach (var parameter in parameters)
                    {
                        var encoded = EncoderGenerator.CreateFor(parameter.ParameterType, parameter.Name, "", "");
                        encoder.Append(encoded.code);
                        encoderSignature += encoded.signature;
                    }
            }

            string returnStatement;
            var decoder = new DecoderGenerator("receivedMessage.Body", "receivedMessage.Header");

            if (returnType == typeof(Task))
                returnStatement = "return;";
            else if (isProperty)
            {
                // must be "Get"
                decoder.Add("result", typeof(object));
                returnStatement = "return (" + BuildTypeString(returnType.GenericTypeArguments[0]) + ")result;";
            }
            else // Task<T>
            {
                decoder.Add("result", returnType.GenericTypeArguments[0]);
                returnStatement = "return result;";
            }

            return @"
        public async " + returnTypeString + @" " + methodInfo.Name + @"(" + string.Join(", ", methodInfo.GetParameters().Select(x => BuildTypeString(x.ParameterType) + " " + x.Name)) + @")
        {
            var sendBody = global::Dbus.Encoder.StartNew();
" + encoder + @"
            var receivedMessage = await connection.SendMethodCall(
                this.path,
                """ + interfaceName + @""",
                """ + callName + @""",
                this.destination,
                sendBody,
                """ + encoderSignature + @"""
            ).ConfigureAwait(false);
            receivedMessage.Signature.AssertEqual(""" + decoder.Signature + @""");
" + decoder.Result + @"            " + returnStatement + @"
        }
";
        }
    }
}
