﻿using System;
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
            var returnTypeString = BuildTypeString(returnType);
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
                }
                foreach (var parameter in parameters)
                {
                    encoder.Append(Indent);
                    encoder.AppendLine("global::Dbus.Encoder.Add(sendBody, ref sendIndex, " + parameter.Name + ");");
                    encoderSignature += SignatureString.For[parameter.ParameterType];
                }
            }

            string returnStatement;
            var decoder = new DecoderGenerator("receivedMessage.Body");

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
                path,
                """ + interfaceName + @""",
                """ + callName + @""",
                destination,
                sendBody,
                """ + encoderSignature + @"""
            );
            assertSignature(receivedMessage.Signature, """ + decoder.Signature + @""");
" + decoder.Result + @"            " + returnStatement + @"
        }
";
        }
    }
}
