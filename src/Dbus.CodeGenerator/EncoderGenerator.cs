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
        private readonly StringBuilder resultBuilder = new StringBuilder();
        private readonly StringBuilder signatureBuilder = new StringBuilder();

        public EncoderGenerator(string body)
            => this.body = body;

        public string Result => resultBuilder.ToString();
        public string Signature => signatureBuilder.ToString();

        public void AddVariant(string name, Type type, string indent = Generator.Indent)
        {
            var (signature, code) = encoder(name, name, type, Generator.Indent);

            var innerGenerator = new EncoderGenerator(body);
            innerGenerator.add($@"(global::Dbus.Signature)""{signature}""", name, typeof(Signature), Generator.Indent);
            signatureBuilder.Append("v");
            resultBuilder.Append(innerGenerator.Result);
            resultBuilder.AppendLine(Generator.Indent + code);
        }

        public void Add(string name, Type type, string indent = Generator.Indent)
            => add(name, name, type, indent);

        private void add(string value, string name, Type type, string indent)
        {
            var (signature, code) = encoder(value, name, type, indent);
            signatureBuilder.Append(signature);
            resultBuilder.Append(indent);
            resultBuilder.AppendLine(code);
        }

        private (string signature, string code) encoder(string value, string name, Type type, string indent)
        {
            if (type == typeof(object))
                return (
                    SignatureString.For[type],
                    body + ".AddVariant(" + value + ");"
                );
            else if (!type.IsConstructedGenericType)
            {
                if (SignatureString.For.ContainsKey(type))
                    return (
                        SignatureString.For[type],
                        body + ".Add(" + value + ");"
                    );
                else
                    return buildFromConstructor(value, name, type, indent);
            }
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var (elementSignature, elementCode) = createMethod(elementType, name + "_e", name + "_e", indent);
                    return (
                        "a" + elementSignature,
                        body + ".Add(" + value + ", " + elementCode + ");"
                    );
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var (keySignature, keyCode) = createMethod(keyType, name + "_k", name + "_k", indent);
                    var (valueSignature, valueCode) = createMethod(valueType, name + "_v", name + "_v", indent);

                    return (
                        "a{" + keySignature + valueSignature + "}",
                        body + ".Add(" + value + ", " + keyCode + ", " + valueCode + ");"
                    );
                }
                else
                    throw new InvalidOperationException("Only IEnumerable and IDictionary are supported as generic type");
            }

        }

        private (string signature, string code) buildFromConstructor(string value, string name, Type type, string indent)
        {
            var constructorParameters = type.GetTypeInfo()
                .GetConstructors()
                .Select(x => x.GetParameters())
                .OrderByDescending(x => x.Length)
                .First()
            ;
            var builder = new StringBuilder();
            builder.AppendLine(body + ".EnsureAlignment(8);");
            var signature = "(";

            foreach (var p in constructorParameters)
            {
                var parameterName = p.Name;
                var propertyName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);
                var encoder = new EncoderGenerator(body);
                encoder.add(value + "." + propertyName, name + "_" + propertyName, p.ParameterType, indent);
                signature += encoder.Signature;
                builder.Append(encoder.Result);
            }

            signature += ")";

            return (signature, builder.ToString());
        }

        private (string signature, string code) createMethod(Type type, string value, string name, string indent)
        {
            if (type == typeof(object))
                return (
                    SignatureString.For[type],
                    body + ".AddVariant"
                );
            else if (SignatureString.For.ContainsKey(type))
                return (
                    SignatureString.For[type],
                    body + ".Add"
                );
            else
            {
                var encoder = new EncoderGenerator(body);
                encoder.add(value, name, type, indent + "    ");
                return (
                    encoder.Signature,
                    "(" + Generator.BuildTypeString(type) + " " + name + @") =>
" + indent + @"{
" + encoder.Result + indent + "}"
                );
            }
        }
    }
}
