using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static StringBuilder provideMethodImplementation(MethodInfo method)
        {
            var builder = new StringBuilder();

            var methodParameters = new List<string>();
            var decoded = new List<(StringBuilder signature, StringBuilder variable)>();
            foreach (var parameter in method.GetParameters())
            {
                if (parameter.ParameterType == typeof(CancellationToken))
                    methodParameters.Add("cancellationToken");
                else
                {
                    var decoder = DecoderGenerator.Create(method.Name + "_" + parameter.Name, parameter.ParameterType);
                    builder.Append(decoder.Delegates);
                    methodParameters.Add(parameter.Name!);
                    var variable = new StringBuilder()
                        .Append(Indent)
                        .Append("var ")
                        .Append(parameter.Name)
                        .Append(" = ")
                        .Append(decoder.DelegateName)
                        .AppendLine(@"(decoder);")
                    ;
                    decoded.Add((decoder.Signature, variable));
                }
            }

            var encoder = new EncoderGenerator("sendBody");
            if (method.ReturnType != typeof(Task)) // Task<T>
            {
                var returnType = method.ReturnType.GenericTypeArguments[0];
                encoder.Add("methodResult", returnType);
            }

            if (encoder.Signature.Length != 0)
                builder
                    .Append(@"
        private static void encode_")
                    .Append(method.Name)
                    .Append("(global::Dbus.Encoder sendBody, ")
                    .Append(BuildTypeString(method.ReturnType.GenericTypeArguments[0]))
                    .AppendLine(@" methodResult)
        {")
                    .Append(encoder.Result)
                    .AppendLine(@"        }")
                ;

            builder
                .Append(@"
        private async global::System.Threading.Tasks.Task handle")
                .Append(method.Name)
                .Append(@"(global::Dbus.MethodCallOptions methodCallOptions, global::Dbus.Decoder decoder, global::System.Threading.CancellationToken cancellationToken)
        {
            decoder.AssertSignature(""")
                .AppendJoin("", decoded.Select(x => x.signature))
                .AppendLine(@""");")
                .AppendJoin(@"", decoded.Select(x => x.variable))
            ;
            if (encoder.Signature.Length != 0)
                builder
                    .Append(Indent)
                    .Append("var methodResult = ")
                ;
            else
                builder.Append(Indent);
            builder
                .Append("await target.")
                .Append(method.Name)
                .Append("(")
                .AppendJoin(", ", methodParameters)
                .Append(@");
            if (methodCallOptions.NoReplyExpected)
                return;")
            ;
            if (encoder.Signature.Length != 0)
                builder
                    .Append(@"
            var sendBody = new global::Dbus.Encoder();
            encode_")
                    .Append(method.Name)
                    .Append(@"(sendBody, methodResult);")
                ;
            builder
                .Append(@"
            await connection.SendMethodReturnAsync(methodCallOptions, ")
                .Append(encoder.Signature.Length != 0 ? "sendBody" : "null")
                .Append(@", """)
                .Append(encoder.Signature)
                .AppendLine(@""", cancellationToken).ConfigureAwait(false);
        }")
            ;

            return builder;
        }
    }
}
