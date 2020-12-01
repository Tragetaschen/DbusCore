using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public class EncoderGenerator
    {
        private readonly string body;

        public EncoderGenerator(string body)
            => this.body = body;

        public StringBuilder Result { get; } = new StringBuilder();
        public StringBuilder Signature { get; } = new StringBuilder();
        public bool IsCompoundValue { get; private set; }

        public void AddVariant(string name, Type type)
        {
            var (signature, code, _) = encoder(name, name, type, Generator.Indent);

            Signature.Append("v");
            Result.Append(buildKnownTypeEncodeCode(Generator.Indent, $@"(global::Dbus.Signature)""{signature}"""));
            Result.Append(code);
        }

        public void Add(string name, Type type)
            => add(name, name, type, Generator.Indent);

        private void add(string value, string name, Type type, string indent)
        {
            var (signature, code, isCompoundValue) = encoder(value, name, type, indent);
            Signature.Append(signature);
            Result.Append(code);
            IsCompoundValue = isCompoundValue;
        }

        private (StringBuilder signature, StringBuilder code, bool isCompoundValue) encoder(string value, string name, Type type, string indent)
        {
            if (SignatureString.For.TryGetValue(type, out var simpleSignature))
            {
                var code = buildKnownTypeEncodeCode(indent, value);
                return (new StringBuilder(simpleSignature), code, false);
            }

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(List<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var encoder = new EncoderGenerator(body);
                    encoder.add(name + "_e", name + "_e", elementType, indent + "    ");
                    var code = buildArrayEncodeCode(indent, value, name, encoder);
                    var signature = new StringBuilder()
                        .Append("a")
                        .Append(encoder.Signature)
                    ;
                    return (signature, code, false);
                }
                else if (genericType == typeof(Dictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var keyEncoder = new EncoderGenerator(body);
                    keyEncoder.add(name + "_kv.Key", name + "_k", keyType, indent + "    ");
                    var valueEncoder = new EncoderGenerator(body);
                    valueEncoder.add(name + "_kv.Value", name + "_v", valueType, indent + "    ");

                    var code = buildDictionaryEncodeCode(indent, value, name, keyEncoder, valueEncoder);
                    var signature = new StringBuilder()
                        .Append("a{")
                        .Append(keyEncoder.Signature)
                        .Append(valueEncoder.Signature)
                        .Append("}")
                    ;
                    return (signature, code, false/*?*/);
                }
                // All other generic types are built from their constructor
            }

            return buildFromConstructor(value, name, type, indent);
        }

        private StringBuilder buildKnownTypeEncodeCode(string indent, string value)
            => new StringBuilder()
                .Append(indent)
                .Append(body)
                .Append(".Add(")
                .Append(value)
                .AppendLine(");")
            ;

        private StringBuilder buildArrayEncodeCode(string indent, string value, string name, EncoderGenerator encoder)
            => new StringBuilder()
                .Append(indent)
                .Append("var ")
                .Append(name)
                .Append("_state = ")
                .Append(body)
                .Append(".StartArray(storesCompoundValues: ")
                .Append(encoder.IsCompoundValue ? "true" : "false")
                .AppendLine(@");")

                .Append(indent)
                .Append("foreach (var ")
                .Append(name)
                .Append("_e in ")
                .Append(value)
                .AppendLine(")")

                .Append(indent)
                .AppendLine("{")

                .Append(encoder.Result)

                .Append(indent)
                .AppendLine("}")

                .Append(indent)
                .Append(body)
                .Append(".FinishArray(")
                .Append(name)
                .AppendLine("_state);")
            ;

        private StringBuilder buildDictionaryEncodeCode(string indent, string value, string name, EncoderGenerator keyEncoder, EncoderGenerator valueEncoder)
            => new StringBuilder()
                .Append(indent)
                .Append("var ")
                .Append(name)
                .Append("_state = ")
                .Append(body)
                .AppendLine(".StartArray(storesCompoundValues: true);")

                .Append(indent)
                .Append("foreach (var ")
                .Append(name)
                .Append("_kv in ")
                .Append(value)
                .AppendLine(")")

                .Append(indent)
                .AppendLine("{")

                .Append(indent)
                .Append("    ")
                .Append(body)
                .AppendLine(".StartCompoundValue();")

                .Append(keyEncoder.Result)
                .Append(valueEncoder.Result)

                .Append(indent)
                .AppendLine("}")

                .Append(indent)
                .Append(body)
                .Append(".FinishArray(")
                .Append(name)
                .AppendLine("_state);")
            ;

        private (StringBuilder signature, StringBuilder code, bool isCompoundValue) buildFromConstructor(string value, string name, Type type, string indent)
        {
            var constructorParameters = type
                .GetConstructors()
                .Select(x => x.GetParameters())
                .OrderByDescending(x => x.Length)
                .First()
            ;
            var isStruct = type.GetCustomAttribute<NoDbusStructAttribute>() == null;

            var builder = new StringBuilder();
            var signature = new StringBuilder();
            if (isStruct)
            {
                signature.Append("(");
                builder.Append(indent)
                    .Append(body)
                    .AppendLine(".StartCompoundValue();")
                ;
            }

            foreach (var p in constructorParameters)
            {
                var parameterName = p.Name!;
                var propertyName = char.ToUpper(parameterName[0]) + parameterName[1..];
                var encoder = new EncoderGenerator(body);
                encoder.add(value + "." + propertyName, name + "_" + propertyName, p.ParameterType, indent);
                signature.Append(encoder.Signature);
                builder.Append(encoder.Result);
            }

            if (isStruct)
                signature.Append(")");

            return (signature, builder, true);
        }
    }
}
