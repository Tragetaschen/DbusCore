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
        private readonly string body;
        private readonly string header;

        private readonly StringBuilder resultBuilder = new StringBuilder();
        private readonly StringBuilder signatureBuilder = new StringBuilder();

        public DecoderGenerator(string body, string header)
        {
            this.body = body;
            this.header = header;
        }

        public string Result => resultBuilder.ToString();
        public string Signature => signatureBuilder.ToString();

        public void Add(string name, Type type, string indent = Generator.Indent)
        {
            if (resultBuilder.Length == 0)
            {
                resultBuilder.Append(indent);
                resultBuilder.AppendLine("var decoderIndex = 0;");
            }
            add(name, type, indent, "decoderIndex");
        }

        private void add(string name, Type type, string indent, string index)
        {
            var (signature, code) = decoder(name, type, indent, body, index);
            signatureBuilder.Append(signature);
            resultBuilder.Append(indent);
            resultBuilder.AppendLine(code);
        }

        private (string signature, string code) decoder(string name, Type type, string indent, string body, string index)
        {
            if (!type.IsConstructedGenericType)
            {
                if (SignatureString.For.ContainsKey(type))
                    return (
                        SignatureString.For[type],
                        "var " + name + " = global::Dbus.Decoder.Get" + type.Name + "(" + body + ", ref " + index + ");"
                    );
                else if (type == typeof(SafeHandle))
                    return (
                        "h",
                        @"var " + name + @"_index = global::Dbus.Decoder.GetInt32(" + body + ", ref " + index + @");
" + indent + @"var " + name + @" = " + header + ".UnixFds[" + name + @"_index];"
                    );
                else if (type == typeof(Stream))
                    return (
                        "h",
                        @"var " + name + @"_index = global::Dbus.Decoder.GetInt32(" + body + ", ref " + index + @");
" + indent + @"var " + name + @" = " + header + ".GetStreamFromFd(" + name + @"_index);"
                    );
                else
                    return buildFromConstructor(name, type, indent, body, index);
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
                        "var " + name + " = global::Dbus.Decoder.GetArray(" + body + ", ref " + index + ", " + code + ");"
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
                        "var " + name + " = global::Dbus.Decoder.GetDictionary(" + body + ", ref " + index + ", " + keyCode + ", " + valueCode + ");"
                    );
                }
                else
                    throw new InvalidOperationException("Only IEnumerable and IDictionary are supported as generic type");
            }

        }

        private (string signature, string code) buildFromConstructor(string name, Type type, string indent, string body, string index)
        {
            var constructorParameters = type.GetTypeInfo()
                .GetConstructors()
                .Select(x => x.GetParameters())
                .OrderByDescending(x => x.Length)
                .First()
            ;
            var builder = new StringBuilder();
            builder.AppendLine("global::Dbus.Alignment.Advance(ref " + index + ", 8);");
            var signature = "(";

            foreach (var p in constructorParameters)
            {
                var decoder = new DecoderGenerator(body, header);
                decoder.add(name + "_" + p.Name, p.ParameterType, indent, index);
                signature += decoder.Signature;
                builder.Append(decoder.Result);
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
                    "global::Dbus.Decoder.Get" + type.Name
                );
            else
            {
                var decoder = new DecoderGenerator(name + "_b", header);
                decoder.add(name + "_inner", type, indent + "    ", name + "_i");
                //var function = decoder(name, type, indent, name + "_b", name + "_i");
                return (
                    decoder.Signature,
                    "(global::System.ReadOnlySpan<byte> " + name + "_b, ref int " + name + @"_i) =>
" + indent + @"{
" + decoder.Result + @"
    " + indent + "return " + name + @"_inner;
" + indent + "}"
                );
            }
        }
    }
}
