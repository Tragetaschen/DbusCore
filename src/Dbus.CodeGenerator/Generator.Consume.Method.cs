using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator;

public static partial class Generator
{
    private static readonly Regex propertyName = new("^(Get|Set)[A-Z].+");

    private static StringBuilder consumeMethod(MethodInfo methodInfo, string interfaceName)
    {
        if (!methodInfo.Name.EndsWith("Async"))
            throw new InvalidOperationException($"The method '{methodInfo.Name}' does not end with 'Async'");
        var callName = methodInfo.Name.Substring(0, methodInfo.Name.Length - "Async".Length);

        var returnType = methodInfo.ReturnType;
        if (!(
            returnType == typeof(Task) ||
            returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
        ))
            throw new InvalidOperationException($"The method '{methodInfo.Name}' does not return a Task type");

        var isProperty = propertyName.IsMatch(callName);
        isProperty &= methodInfo.GetCustomAttribute<DbusMethodAttribute>() == null;

        var parameters = methodInfo.GetParameters();
        var encodeParameters = new List<ParameterInfo>();

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
                encoder.Add($@"""{callName[3 /* "Get" or "Set" */..]}""", typeof(string));
                interfaceName = "org.freedesktop.DBus.Properties";
                callName = callName.Substring(0, 3); // "Get" or "Set"
                foreach (var parameter in parameters)
                    if (parameter.ParameterType == typeof(CancellationToken))
                        cancellationTokenName = parameter.Name!;
                    else
                    {
                        encoder.AddVariant(parameter.Name!, parameter.ParameterType);
                        encodeParameters.Add(parameter);
                    }
            }
            else
                foreach (var parameter in parameters)
                    if (parameter.ParameterType == typeof(CancellationToken))
                        cancellationTokenName = parameter.Name!;
                    else
                    {
                        encoder.Add(parameter.Name!, parameter.ParameterType);
                        encodeParameters.Add(parameter);
                    }
        }

        DecoderGenerator decoderGenerator;
        StringBuilder returnStatement;

        if (returnType == typeof(Task))
        {
            decoderGenerator = DecoderGenerator.Empty();
            returnStatement = new StringBuilder("return;");
        }
        else if (isProperty)
        {
            // must be "Get"
            decoderGenerator = DecoderGenerator.Create("result_" + methodInfo.Name, typeof(object));
            returnStatement = new StringBuilder()
                .Append("return (")
                .Append(BuildTypeString(returnType.GenericTypeArguments[0]))
                .Append(")")
                .Append(decoderGenerator.DelegateName)
                .Append("(decoder);")
             ;
        }
        else // Task<T>
        {
            decoderGenerator = DecoderGenerator.Create("result_" + methodInfo.Name, returnType.GenericTypeArguments[0]);
            returnStatement = new StringBuilder()
                .Append("return ")
                .Append(decoderGenerator.DelegateName)
                .Append("(decoder);")
            ;
        }

        var builder = new StringBuilder();
        if (encoder.Signature.Length != 0)
        {
            builder
                .Append(@"
        private static void encode_")
                .Append(methodInfo.Name)
                .Append("(global::Dbus.Encoder sendBody")
            ;
            if (encodeParameters.Count > 0)
                builder
                    .Append(", ")
                    .AppendJoin(", ", encodeParameters.Select(parameter => new StringBuilder()
                        .Append(BuildTypeString(parameter.ParameterType))
                        .Append(" ")
                        .Append(parameter.Name))
                    )
                ;
            builder
                .AppendLine(@")
        {")
                .Append(encoder.Result)
                .AppendLine(@"        }")
            ;
        }

        builder
            .Append(decoderGenerator.Delegates)
            .Append(@"
        public async ")
            .Append(BuildTypeString(returnType))
            .Append(" ")
            .Append(methodInfo.Name)
            .Append("(")
            .AppendJoin(", ", parameters.Select(parameter => new StringBuilder()
                .Append(BuildTypeString(parameter.ParameterType))
                .Append(" ")
                .Append(parameter.Name)
            ))
            .Append(@")
        {")
        ;
        if (encoder.Signature.Length != 0)
        {
            builder
                .Append(@"
            var sendBody = new global::Dbus.Encoder();
            encode_")
                .Append(methodInfo.Name)
                .Append("(sendBody")
            ;
            if (encodeParameters.Count > 0)
                builder
                    .Append(", ")
                    .AppendJoin(", ", encodeParameters.Select(parameter => parameter.Name))
                ;
            builder.Append(");");
        }
        builder
            .Append(@"
            var decoder = await connection.SendMethodCall(
                this.path,
                """)
            .Append(interfaceName)
            .Append(@""",
                """)
            .Append(callName)
            .Append(@""",
                this.destination,
                ")
            .Append(encoder.Signature.Length != 0 ? "sendBody" : "null")
            .Append(@",
                """)
            .Append(encoder.Signature)
            .Append(@""",
                ")
            .Append(cancellationTokenName)
            .Append(@"
            ).ConfigureAwait(false);
            using (decoder)
            {
                decoder.AssertSignature(""")
            .Append(decoderGenerator.Signature)
            .Append(@""");
                ")
            .Append(returnStatement)
            .AppendLine(@"
            }
        }");
        return builder;
    }
}
