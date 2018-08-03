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

        public void Add(string name, Type type, string indent = Generator.Indent)
            => add(name, type, indent);

        private void add(string name, Type type, string indent)
        {
            var (signature, code) = generateDecoder(name, type, indent);
            signatureBuilder.Append(signature);
            resultBuilder.Append(indent);
            resultBuilder.AppendLine(code);
        }

        private (string signature, string code) generateDecoder(string name, Type type, string indent)
        {
            if (!type.IsConstructedGenericType)
            {
                if (SignatureString.For.ContainsKey(type))
                    return (
                        SignatureString.For[type],
                        "var " + name + " = " + decoder + ".Get" + type.Name + "();"
                    );
                else if (type == typeof(SafeHandle))
                    return (
                        "h",
                        @"var " + name + @"_index = " + decoder + @".GetInt32();
" + indent + @"var " + name + @" = " + message + ".UnixFds[" + name + @"_index];"
                    );
                else if (type == typeof(Stream))
                    return (
                        "h",
                        @"var " + name + @"_index = " + decoder + @".GetInt32();
" + indent + @"var " + name + @" = " + message + ".GetStream(" + name + @"_index);"
                    );
                else
                    return buildFromConstructor(name, type, indent);
            }
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var (signature, code) = createMethod(elementType, name + "_e", indent);
                    return (
                        "a" + signature,
                        "var " + name + " = " + decoder + ".GetArray(" + code + ");"
                    );
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var (keySignature, keyCode) = createMethod(keyType, name + "_k", indent);
                    var (valueSignature, valueCode) = createMethod(valueType, name + "_v", indent);

                    return (
                        "a{" + keySignature + valueSignature + "}",
                        "var " + name + " = " + decoder + ".GetDictionary(" + keyCode + ", " + valueCode + ");"
                    );
                }
                else
                    throw new InvalidOperationException("Only IEnumerable and IDictionary are supported as generic type");
            }

        }

        private (string signature, string code) buildFromConstructor(string name, Type type, string indent)
        {
            var constructorParameters = type.GetTypeInfo()
                .GetConstructors()
                .Select(x => x.GetParameters())
                .OrderByDescending(x => x.Length)
                .First()
            ;
            var builder = new StringBuilder();
            builder.AppendLine(decoder + ".AdvanceToStruct();");
            var signature = "(";

            foreach (var p in constructorParameters)
            {
                var decoderGenerator = new DecoderGenerator(decoder, message);
                decoderGenerator.add(name + "_" + p.Name, p.ParameterType, indent);
                signature += decoderGenerator.Signature;
                builder.Append(decoderGenerator.Result);
            }

            signature += ")";
            builder.Append(indent);
            builder.Append("var " + name + " = new " + Generator.BuildTypeString(type) + "(");
            builder.Append(string.Join(", ", constructorParameters.Select(x => name + "_" + x.Name)));
            builder.Append(");");

            return (signature, builder.ToString());
        }

        private (string signature, string code) createMethod(Type type, string name, string indent)
        {
            if (SignatureString.For.ContainsKey(type))
                return (
                    SignatureString.For[type],
                    decoder + ".Get" + type.Name
                );
            else
            {
                var decoderGenerator = new DecoderGenerator(decoder, message);
                decoderGenerator.add(name + "_inner", type, indent + "    ");
                return (
                    decoderGenerator.Signature,
                    @"() =>
" + indent + @"{
" + decoderGenerator.Result + @"
    " + indent + "return " + name + @"_inner;
" + indent + "}"
                );
            }
        }
    }
}
