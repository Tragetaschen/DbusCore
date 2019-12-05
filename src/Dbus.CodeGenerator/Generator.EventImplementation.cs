using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        public static (string subscription, string implementation) GenerateEventImplementation(EventInfo eventInfo, string interfaceName)
        {
            if (eventInfo.EventHandlerType == null)
                throw new InvalidOperationException("EventInfo has no handler type");

            var subscription = new StringBuilder();
            subscription.Append(Indent);
            subscription.AppendLine("eventSubscriptions.Add(connection.RegisterSignalHandler(");
            subscription.Append(Indent);
            subscription.Append("    ");
            subscription.AppendLine("this.path,");
            subscription.Append(Indent);
            subscription.Append("    ");
            subscription.AppendLine(@"""" + interfaceName + @""",");
            subscription.Append(Indent);
            subscription.Append("    ");
            subscription.AppendLine(@"""" + eventInfo.Name + @""",");
            subscription.Append(Indent);
            subscription.Append("    ");
            subscription.AppendLine("this.handle" + eventInfo.Name);
            subscription.Append(Indent);
            subscription.AppendLine("));");


            var invocationParameters = new List<string>();
            var decoder = new DecoderGenerator("receivedMessage.Decoder", "receivedMessage");

            if (eventInfo.EventHandlerType.IsConstructedGenericType)
            {
                var arguments = eventInfo.EventHandlerType.GenericTypeArguments;
                for (var i = 0; i < arguments.Length; ++i)
                {
                    var invocationParameter = "decoded" + i;
                    decoder.Add(invocationParameter, arguments[i], Indent);
                    invocationParameters.Add(invocationParameter);
                }
            }

            var implementation = new StringBuilder();
            implementation.Append("        ");
            implementation.Append("public event ");
            implementation.Append(BuildTypeString(eventInfo.EventHandlerType));
            implementation.Append(" ");
            implementation.Append(eventInfo.Name);
            implementation.AppendLine(";");
            implementation.Append("        ");
            implementation.Append("private void handle");
            implementation.Append(eventInfo.Name);
            implementation.AppendLine("(global::Dbus.ReceivedMessage receivedMessage)");
            implementation.Append("        ");
            implementation.AppendLine("{");
            implementation.Append(Indent);
            implementation.AppendLine(@"receivedMessage.AssertSignature(""" + decoder.Signature + @""");");
            implementation.Append(decoder.Result);
            implementation.Append(Indent);
            implementation.AppendLine(eventInfo.Name + "?.Invoke(" + string.Join(", ", invocationParameters) + ");");
            implementation.Append("        ");
            implementation.AppendLine("}");

            return (subscription.ToString(), implementation.ToString());
        }
    }
}
