﻿using System;
using System.Linq;
using System.Reflection;
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

            var parameters = methodInfo.GetParameters();

            if (isProperty)
                if (callName.StartsWith("Get"))
                    isProperty &= parameters.Length == 0;
                else if (callName.StartsWith("Set"))
                    isProperty &= parameters.Length == 1;

            var encoder = new EncoderGenerator("sendBody");

            if (parameters.Length > 0 || isProperty)
            {
                if (isProperty)
                {
                    encoder.Add($@"""{interfaceName}""", typeof(string));
                    encoder.Add($@"""{callName.Substring(3 /* "Get" or "Set" */)}""", typeof(string));
                    interfaceName = "org.freedesktop.DBus.Properties";
                    callName = callName.Substring(0, 3); // "Get" or "Set"
                    foreach (var parameter in parameters)
                        encoder.AddVariant(parameter.Name, parameter.ParameterType);
                }
                else
                    foreach (var parameter in parameters)
                        encoder.Add(parameter.Name, parameter.ParameterType);
            }

            string returnStatement;
            var decoder = new DecoderGenerator("decoder", "receivedMessage.Header");

            if (returnType == typeof(Task))
                returnStatement = "return;";
            else if (isProperty)
            {
                // must be "Get"
                decoder.Add("result", typeof(object), Indent + "    ");
                returnStatement = "return (" + BuildTypeString(returnType.GenericTypeArguments[0]) + ")result;";
            }
            else // Task<T>
            {
                decoder.Add("result", returnType.GenericTypeArguments[0], Indent + "    ");
                returnStatement = "return result;";
            }

            var createFunction = "";

            if (returnType != typeof(Task))
                createFunction = @"
            " + BuildTypeString(methodInfo.ReturnType.GenericTypeArguments[0]) + @" createResult(global::Dbus.Decoder decoder)
            {
" + decoder.Result + @"
                " + returnStatement + @"
            }
            var createdResult = createResult(new global::Dbus.Decoder(receivedMessage.Body.Memory, receivedMessage.BodyLength));";

            return @"
        public async " + returnTypeString + @" " + methodInfo.Name + @"(" + string.Join(", ", methodInfo.GetParameters().Select(x => BuildTypeString(x.ParameterType) + " " + x.Name)) + @")
        {
            var sendBody = new global::Dbus.Encoder();
" + encoder.Result + @"
            var receivedMessage = await connection.SendMethodCall(
                this.path,
                """ + interfaceName + @""",
                """ + callName + @""",
                this.destination,
                sendBody,
                """ + encoder.Signature + @"""
            ).ConfigureAwait(false);
            receivedMessage.Signature.AssertEqual(""" + decoder.Signature + @""");
" + (returnType == typeof(Task) ? "" : createFunction) + @"
            receivedMessage.Body.Dispose();
            " + (returnType == typeof(Task) ? "return;" : "return createdResult;") + @"
        }
";
        }
    }
}
