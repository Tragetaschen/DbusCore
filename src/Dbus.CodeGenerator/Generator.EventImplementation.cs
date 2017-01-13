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
        public static Tuple<string, string> generateEventImplementation(EventInfo eventInfo, string interfaceName)
        {
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
            subscription.AppendLine("handle" + eventInfo.Name);
            subscription.Append(Indent);
            subscription.AppendLine("));");


            var invocationParameter = "";
            var decoder = new DecoderGenerator("body");

            if (eventInfo.EventHandlerType.IsConstructedGenericType)
            {
                invocationParameter = "decoded";
                decoder.Add(invocationParameter, eventInfo.EventHandlerType.GenericTypeArguments[0]);
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
            implementation.AppendLine("(global::Dbus.MessageHeader header, byte[] body)");
            implementation.Append("        ");
            implementation.AppendLine("{");
            implementation.Append(Indent);
            implementation.AppendLine(@"assertSignature(header.BodySignature, """ + decoder.Signature + @""");");
            implementation.Append(decoder.Result);
            implementation.Append(Indent);
            implementation.AppendLine(eventInfo.Name + "?.Invoke(" + invocationParameter + ");");
            implementation.Append("        ");
            implementation.AppendLine("}");

            return Tuple.Create(subscription.ToString(), implementation.ToString());
        }
    }
}
