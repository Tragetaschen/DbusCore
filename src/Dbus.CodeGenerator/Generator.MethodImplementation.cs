using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static readonly Regex propertyName = new Regex("^(Get|Set)[A-Z].+");

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
                {
                    if (parameters.Length == 0)
                        isProperty &= true;
                    else if (parameters[0].ParameterType == typeof(CancellationToken))
                        isProperty &= true;
                    else
                        isProperty = false;
                }
                else if (callName.StartsWith("Set"))
                {
                    if (parameters.Length == 1 && parameters[0].ParameterType != typeof(CancellationToken))
                        isProperty &= true;
                    else if (parameters.Length == 2 && parameters[1].ParameterType == typeof(CancellationToken))
                        isProperty &= true;
                    else
                        isProperty = false;
                }

            var encoder = new EncoderGenerator("sendBody");
            var cancellationTokenName = "default(global::System.Threading.CancellationToken)";

            if (parameters.Length > 0 || isProperty)
            {
                if (isProperty)
                {
                    encoder.Add($@"""{interfaceName}""", typeof(string));
                    encoder.Add($@"""{callName.Substring(3 /* "Get" or "Set" */)}""", typeof(string));
                    interfaceName = "org.freedesktop.DBus.Properties";
                    callName = callName.Substring(0, 3); // "Get" or "Set"
                    foreach (var parameter in parameters)
                        if (parameter.ParameterType == typeof(CancellationToken))
                            cancellationTokenName = parameter.Name;
                        else
                            encoder.AddVariant(parameter.Name, parameter.ParameterType);
                }
                else
                    foreach (var parameter in parameters)
                        if (parameter.ParameterType == typeof(CancellationToken))
                            cancellationTokenName = parameter.Name;
                        else
                            encoder.Add(parameter.Name, parameter.ParameterType);
            }

            string returnStatement;
            var decoder = new DecoderGenerator("decoder", "receivedMessage");

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
            " + BuildTypeString(methodInfo.ReturnType.GenericTypeArguments[0]) + @" decode(global::Dbus.Decoder decoder)
            {
" + decoder.Result + @"
                " + returnStatement + @"
            }
";

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
                """ + encoder.Signature + @""",
                " + cancellationTokenName + @"
            ).ConfigureAwait(false);
" + (returnType == typeof(Task) ? "" : createFunction) + @"
            using (receivedMessage)
            {
                receivedMessage.AssertSignature(""" + decoder.Signature + @""");
                " + (returnType == typeof(Task) ? "return;" : "return decode(receivedMessage.Decoder);") + @"
            }
        }
";
        }
    }
}
