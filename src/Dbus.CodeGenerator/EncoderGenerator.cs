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
        public bool IsCompoundValue { get; private set; }

        public void AddVariant(string name, Type type)
        {
            var (signature, code, _) = encoder(name, name, type, Generator.Indent);

            var innerGenerator = new EncoderGenerator(body);
            innerGenerator.add($@"(global::Dbus.Signature)""{signature}""", name, typeof(Signature), Generator.Indent);
            signatureBuilder.Append("v");
            resultBuilder.Append(innerGenerator.Result);
            resultBuilder.AppendLine(code);
        }

        public void Add(string name, Type type, string indent = Generator.Indent)
            => add(name, name, type, indent);

        private void add(string value, string name, Type type, string indent)
        {
            var (signature, code, isCompoundValue) = encoder(value, name, type, indent);
            signatureBuilder.Append(signature);
            resultBuilder.AppendLine(code);
            IsCompoundValue = isCompoundValue;
        }

        private (string signature, string code, bool isCompoundValue) encoder(string value, string name, Type type, string indent)
        {
            if (SignatureString.For.ContainsKey(type))
                return (
                    SignatureString.For[type],
                    indent + body + ".Add(" + value + ");",
                    false
                );

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>) || genericType == typeof(IList<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var encoder = new EncoderGenerator(body);
                    encoder.add(name + "_e", name + "_e", elementType, indent + "    ");

                    return (
                        "a" + encoder.Signature,
                        indent + "var " + name + "_state = " + body + ".StartArray(storesCompoundValues: " + (encoder.IsCompoundValue ? "true" : "false") + @");" + Environment.NewLine +
                        indent + "foreach (var " + name + "_e in " + value + ")" + Environment.NewLine +
                        indent + "{" + Environment.NewLine +
                        encoder.Result +
                        indent + "}" + Environment.NewLine +
                        indent + body + ".FinishArray(" + name + "_state);",
                        false
                    );
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var keyEncoder = new EncoderGenerator(body);
                    keyEncoder.add(name + "_kv.Key", name + "_k", keyType, indent + "    ");
                    var valueEncoder = new EncoderGenerator(body);
                    valueEncoder.add(name + "_kv.Value", name + "_v", valueType, indent + "    ");

                    return (
                        "a{" + keyEncoder.Signature + valueEncoder.Signature + "}",
                        indent + "var " + name + "_state = " + body + ".StartArray(storesCompoundValues: true);" + Environment.NewLine +
                        indent + "foreach (var " + name + "_kv in " + value + ")" + Environment.NewLine +
                        indent + "{" + Environment.NewLine +
                        indent + "    " + body + ".StartCompoundValue();" + Environment.NewLine +
                        keyEncoder.Result +
                        valueEncoder.Result +
                        indent + "}" + Environment.NewLine +
                        indent + body + ".FinishArray(" + name + "_state);" + Environment.NewLine,
                        false // ?
                    );
                }
                // All other generic types are built from their constructor
            }

            return buildFromConstructor(value, name, type, indent);
        }

        private (string signature, string code, bool isCompoundValue) buildFromConstructor(string value, string name, Type type, string indent)
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
                builder.AppendLine(indent + body + ".StartCompoundValue();");
                signature += "(";
            }

            foreach (var p in constructorParameters)
            {
                var parameterName = p.Name!;
                var propertyName = char.ToUpper(parameterName[0]) + parameterName.Substring(1);
                var encoder = new EncoderGenerator(body);
                encoder.add(value + "." + propertyName, name + "_" + propertyName, p.ParameterType, indent);
                signature += encoder.Signature;
                builder.Append(encoder.Result);
            }

            if (isStruct)
                signature += ")";

            return (signature, builder.ToString(), true);
        }
    }
}
