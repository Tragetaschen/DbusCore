using System;
using System.Collections.Generic;
using System.Linq;
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

            var implementation = new StringBuilder();
            implementation.Append("        ");
            implementation.Append("public event ");
            implementation.Append(BuildTypeString(eventInfo.EventHandlerType));
            implementation.Append(" ");
            implementation.Append(eventInfo.Name);
            implementation.AppendLine(";");

            var parameters = new List<(string name, DecoderGenerator decoder)>();
            if (eventInfo.EventHandlerType.IsConstructedGenericType)
            {
                var arguments = eventInfo.EventHandlerType.GenericTypeArguments;
                for (var i = 0; i < arguments.Length; ++i)
                {
                    var invocationParameter = "decoded" + i + "_" + eventInfo.Name;
                    var decoder = DecoderGenerator.Create(invocationParameter, arguments[i]);
                    parameters.Add((invocationParameter, decoder));
                }
            }

            foreach (var (_, decoder) in parameters)
            {
                implementation.AppendJoin("", decoder.Delegates);
                implementation.AppendLine();
            }

            implementation.Append("        ");
            implementation.Append("private void handle");
            implementation.Append(eventInfo.Name);
            implementation.AppendLine("(global::Dbus.ReceivedMessage receivedMessage)");
            implementation.Append("        ");
            implementation.AppendLine("{");
            implementation.Append(Indent);
            implementation.Append(@"receivedMessage.AssertSignature(""");
            implementation.AppendJoin("", parameters.Select(x => x.decoder.Signature));
            implementation.AppendLine(@""");");
            foreach (var (invocationParameter, decoder) in parameters)
            {
                implementation.Append("            ");
                implementation.AppendLine("var " + invocationParameter + " = " + decoder.DelegateName + "(receivedMessage.Decoder);");
            }
            implementation.Append(Indent);
            implementation.Append(eventInfo.Name + "?.Invoke(");
            implementation.AppendJoin(", ", parameters.Select(x => x.name));
            implementation.AppendLine(");");
            implementation.Append("        ");
            implementation.AppendLine("}");

            return (subscription.ToString(), implementation.ToString());
        }
    }
}
