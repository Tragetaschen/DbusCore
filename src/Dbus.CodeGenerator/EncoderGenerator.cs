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
        private string index;
        private readonly StringBuilder resultBuilder = new StringBuilder();
        private readonly StringBuilder signatureBuilder = new StringBuilder();

        public EncoderGenerator(string body)
            : this(body, "")
        { }

        public EncoderGenerator(string body, string index)
        {
            this.body = body;
            this.index = index;
        }

        public string Result => resultBuilder.ToString();
        public string Signature => signatureBuilder.ToString();

        public void AddVariant(string name, Type type)
        {
            ensureSendIndex();

            var variantInfo = encoder(name, name, type, Generator.Indent, body);

            var innerGenerator = new EncoderGenerator(body, index);
            innerGenerator.add($@"(global::Dbus.Signature)""{variantInfo.signature}""", name, typeof(Signature), Generator.Indent);
            signatureBuilder.Append("v");
            resultBuilder.Append(innerGenerator.Result);
            resultBuilder.AppendLine(Generator.Indent + variantInfo.code);
        }

        public void Add(string name, Type type)
        {
            ensureSendIndex();
            add(name, name, type, Generator.Indent);
        }

        private void ensureSendIndex()
        {
            if (index != "")
                return;
            resultBuilder.Append(Generator.Indent);
            resultBuilder.AppendLine("var sendIndex = 0;");
            index = "sendIndex";
        }

        private void add(string value, string name, Type type, string indent)
        {
            var function = encoder(value, name, type, indent, body);
            signatureBuilder.Append(function.signature);
            resultBuilder.Append(indent);
            resultBuilder.AppendLine(function.code);
        }

        private (string signature, string code) encoder(string value, string name, Type type, string indent, string body)
        {
            if (type == typeof(object))
                return (
                    SignatureString.For[type],
                    "global::Dbus.Encoder.AddVariant(" + body + ", ref " + index + ", " + value + ");"
                );
            else if (!type.IsConstructedGenericType)
            {
                if (SignatureString.For.ContainsKey(type))
                    return (
                        SignatureString.For[type],
                        "global::Dbus.Encoder.Add(" + body + ", ref " + index + ", " + value + ");"
                    );
                else
                    return buildFromConstructor(value, name, type, indent, body);
            }
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementFunction = createMethod(elementType, name + "_e", name + "_e", indent);
                    return (
                        "a" + elementFunction.signature,
                        "global::Dbus.Encoder.Add(" + body + ", ref " + index + ", " + value + ", " + elementFunction.code + ");"
                    );
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var keyFunction = createMethod(keyType, name + "_k", name + "_k", indent);
                    var valueFunction = createMethod(valueType, name + "_v", name + "_v", indent);

                    return (
                        "a{" + keyFunction.signature + valueFunction.signature + "}",
                        "global::Dbus.Encoder.Add(" + body + ", ref " + index + ", " + value + ", " + keyFunction.code + ", " + valueFunction.code + ");"
                    );
                }
                else
                    throw new InvalidOperationException("Only IEnumerable and IDictionary are supported as generic type");
            }

        }

        private (string signature, string code) buildFromConstructor(string value, string name, Type type, string indent, string body)
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
                var parameterName = p.Name;
                var propertyName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);
                var encoder = new EncoderGenerator(body, index);
                encoder.add(value + "." + propertyName, name + "_" + propertyName, p.ParameterType, indent);
                signature += encoder.Signature;
                builder.Append(encoder.Result);
            }

            signature += ")";

            return (signature, builder.ToString());
        }

        private static (string signature, string code) createMethod(Type type, string value, string name, string indent)
        {
            if (type == typeof(object))
                return (
                    SignatureString.For[type],
                    "global::Dbus.Encoder.AddVariant"
                );
            else if (SignatureString.For.ContainsKey(type))
                return (
                    SignatureString.For[type],
                    "global::Dbus.Encoder.Add"
                );
            else
            {
                var encoder = new EncoderGenerator(name + "_b", name + "_i");
                encoder.add(value, name, type, indent + "    ");
                return (
                    encoder.Signature,
                    "(global::System.Collections.Generic.List<byte> " + name + "_b, ref int " + name + @"_i, " + Generator.BuildTypeString(type) + " " + name + @") =>
" + indent + @"{
" + encoder.Result + indent + "}"
                );
            }
        }
    }
}
