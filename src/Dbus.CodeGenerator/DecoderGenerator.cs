using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Dbus.CodeGenerator
{
    public class DecoderGenerator
    {
        private readonly string decoder;
        private readonly string message;

        private readonly StringBuilder resultBuilder = new StringBuilder();
        private readonly StringBuilder signatureBuilder = new StringBuilder();

        public DecoderGenerator(string decoder, string message)
        {
            this.decoder = decoder;
            this.message = message;
        }

        public string Result => resultBuilder.ToString();
        public string Signature => signatureBuilder.ToString();
        public bool IsCompoundValue { get; private set; }

        public void Add(string name, Type type, string indent)
        {
            var (signature, code, isCompoundValue) = generateDecoder(name, type, indent);
            IsCompoundValue = isCompoundValue;
            signatureBuilder.Append(signature);
            resultBuilder.AppendLine(code);
        }

        private (string signature, string code, bool isCompoundValue) generateDecoder(string name, Type type, string indent)
        {
            if (!type.IsConstructedGenericType)
            {
                if (SignatureString.For.ContainsKey(type))
                    return (
                        SignatureString.For[type],
                        indent + "var " + name + " = " + decoder + ".Get" + type.Name + "();",
                        false
                    );
                else if (type == typeof(SafeHandle))
                    return (
                        "h",
                        indent + @"var " + name + @"_index = " + decoder + ".GetInt32();\n" +
                        indent + @"var " + name + @" = " + message + ".UnixFds[" + name + @"_index];",
                        false
                    );
                else if (type == typeof(Stream))
                    return (
                        "h",
                        indent + @"var " + name + @"_index = " + decoder + ".GetInt32();\n" +
                        indent + @"var " + name + @" = " + message + ".GetStream(" + name + @"_index);",
                        false
                    );
                else
                    return buildFromConstructor(name, type, indent);
            }
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>) || genericType == typeof(IList<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var (signature, code, isCompoundValue) = createMethod(elementType, name + "_e", indent);
                    return (
                        "a" + signature,
                        indent + "var " + name + " = " + decoder + ".GetArray(" + code + ", " + (isCompoundValue ? "true" : "false") + ");",
                        false
                    );
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var (keySignature, keyCode, _) = createMethod(keyType, name + "_k", indent);
                    var (valueSignature, valueCode, _) = createMethod(valueType, name + "_v", indent);

                    return (
                        "a{" + keySignature + valueSignature + "}",
                        indent + "var " + name + " = " + decoder + ".GetDictionary(" + keyCode + ", " + valueCode + ");",
                        false
                    );
                }
                else
                    throw new InvalidOperationException("Only IEnumerable, IList and IDictionary are supported as generic type");
            }

        }

        private (string signature, string code, bool isCompoundValue) buildFromConstructor(string name, Type type, string indent)
        {
            var constructorParameters = type.GetTypeInfo()
                .GetConstructors()
                .Select(x => x.GetParameters())
                .OrderByDescending(x => x.Length)
                .First()
            ;
            var isStruct = type.GetCustomAttribute<NoDbusStructAttribute>() == null;
            var builder = new StringBuilder();
            var signature = "";

            if (isStruct)
            {
                builder.Append(indent);
                builder.AppendLine(decoder + ".AdvanceToCompoundValue();");
                signature += "(";
            }

            foreach (var p in constructorParameters)
            {
                var decoderGenerator = new DecoderGenerator(decoder, message);
                decoderGenerator.Add(name + "_" + p.Name, p.ParameterType, indent);
                signature += decoderGenerator.Signature;
                builder.Append(decoderGenerator.Result);
            }

            if (isStruct)
                signature += ")";

            builder.Append(indent);
            builder.Append("var " + name + " = new " + Generator.BuildTypeString(type) + "(");
            builder.Append(string.Join(", ", constructorParameters.Select(x => name + "_" + x.Name)));
            builder.Append(");");

            return (signature, builder.ToString(), true);
        }

        private (string signature, string code, bool isCompoundValue) createMethod(Type type, string name, string indent)
        {
            if (SignatureString.For.ContainsKey(type))
                return (
                    SignatureString.For[type],
                    decoder + ".Get" + type.Name,
                    false
                );
            else
            {
                var decoderGenerator = new DecoderGenerator(decoder, message);
                decoderGenerator.Add(name + "_inner", type, indent + "    ");
                return (
                    decoderGenerator.Signature,
                    @"() =>
" + indent + @"{
" + decoderGenerator.Result + @"
" + indent + "    " + @"return " + name + @"_inner;
" + indent + "}",
                    decoderGenerator.IsCompoundValue
                );
            }
        }
    }
}
