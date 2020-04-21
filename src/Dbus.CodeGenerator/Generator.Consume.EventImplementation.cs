using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder consumeEventImplementation(EventInfo eventInfo)
        {
            if (eventInfo.EventHandlerType == null)
                throw new InvalidOperationException("EventInfo has no handler type");

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

            var builder = new StringBuilder()
                .Append(@"
        public event ")
                .Append(BuildTypeString(eventInfo.EventHandlerType))
                .Append(" ")
                .Append(eventInfo.Name)
                .AppendLine(";")
             ;

            foreach (var (_, decoder) in parameters)
                builder.Append(decoder.Delegates);

            builder.Append(@"
        private void handle")
                .Append(eventInfo.Name)
                .Append(@"(global::Dbus.Decoder decoder)
        {
            decoder.AssertSignature(""")
                .AppendJoin("", parameters.Select(parameter => parameter.decoder.Signature))
                .Append(@""");")
            ;
            foreach (var (invocationParameter, decoder) in parameters)
                builder.Append(@"
            var ")
                    .Append(invocationParameter)
                    .Append(" = ")
                    .Append(decoder.DelegateName)
                    .Append("(decoder);")
                ;
            builder
                .AppendLine()
                .Append(Indent)
                .Append(eventInfo.Name)
                .Append("?.Invoke(")
                .AppendJoin(", ", parameters.Select(parameter => parameter.name))
                .AppendLine(@");
        }");
            ;

            return builder;
        }
    }
}
