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
            subscription.Append(indent);
            subscription.AppendLine("eventSubscriptions.Add(connection.RegisterSignalHandler(");
            subscription.Append(indent);
            subscription.Append("    ");
            subscription.AppendLine("this.path,");
            subscription.Append(indent);
            subscription.Append("    ");
            subscription.AppendLine(@"""" + interfaceName + @""",");
            subscription.Append(indent);
            subscription.Append("    ");
            subscription.AppendLine(@"""" + eventInfo.Name + @""",");
            subscription.Append(indent);
            subscription.Append("    ");
            subscription.AppendLine("handle" + eventInfo.Name);
            subscription.Append(indent);
            subscription.AppendLine("));");


            var signature = "";
            var decoder = new StringBuilder();
            if (eventInfo.EventHandlerType.IsConstructedGenericType)
            {
                var bodyType = eventInfo.EventHandlerType.GenericTypeArguments[0];
                signature = signatures[bodyType];
                decoder.Append(indent);
                decoder.AppendLine("var index = 0;");
                decoder.Append(indent);
                decoder.AppendLine("var decoded = global::Dbus.Decoder.Get" + bodyType.Name + "(body, ref index);");
                decoder.Append(indent);
                decoder.AppendLine(eventInfo.Name + "?.Invoke(decoded);");
            }
            else
            {
                decoder.Append(indent);
                decoder.AppendLine(eventInfo.Name + "?.Invoke();");
            }

            var implementation = new StringBuilder();
            implementation.Append("        ");
            implementation.Append("public event ");
            implementation.Append(buildTypeString(eventInfo.EventHandlerType));
            implementation.Append(" ");
            implementation.Append(eventInfo.Name);
            implementation.AppendLine(";");
            implementation.Append("        ");
            implementation.Append("private void handle");
            implementation.Append(eventInfo.Name);
            implementation.AppendLine("(global::Dbus.MessageHeader header, byte[] body)");
            implementation.Append("        ");
            implementation.AppendLine("{");
            implementation.Append(indent);
            implementation.AppendLine(@"assertSignature(header.BodySignature, """ + signature + @""");");
            implementation.Append(decoder);
            implementation.Append("        ");
            implementation.AppendLine("}");

            return Tuple.Create(subscription.ToString(), implementation.ToString());
        }
    }
}
