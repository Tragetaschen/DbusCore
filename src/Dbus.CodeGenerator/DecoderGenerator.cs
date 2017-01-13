using System;
using System.Collections.Generic;
using System.Text;

namespace Dbus.CodeGenerator
{
    public class DecoderGenerator
    {
        private readonly string body;

        private readonly StringBuilder resultBuilder = new StringBuilder();
        private readonly StringBuilder signatureBuilder = new StringBuilder();

        public DecoderGenerator(string body)
        {
            this.body = body;
        }

        public string Result => resultBuilder.ToString();
        public string Signature => signatureBuilder.ToString();

        public void Add(string name, Type type)
        {
            if (resultBuilder.Length == 0)
            {
                resultBuilder.Append(Generator.Indent);
                resultBuilder.AppendLine("var decoderIndex = 0;");
            }
            add(name, type, Generator.Indent, "decoderIndex");
        }

        private void add(string name, Type type, string indent, string index)
        {
            var function = decoder(name, type, indent, body, index);
            signatureBuilder.Append(function.Item1);
            resultBuilder.Append(indent);
            resultBuilder.AppendLine("var " + name + " = " + function.Item2 + ";");
        }

        private static Tuple<string, string> decoder(string name, Type type, string indent, string body, string index)
        {
            if (!type.IsConstructedGenericType)
                return Tuple.Create(
                    SignatureString.For[type],
                    "global::Dbus.Decoder.Get" + type.Name + "(" + body + ", ref " + index + ")"
                );
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementFunction = createMethod(elementType, name + "_e", indent + "    ");
                    return Tuple.Create(
                        "a" + elementFunction.Item1,
                        "global::Dbus.Decoder.GetArray(" + body + ", ref " + index + ", " + elementFunction.Item2 + ")"
                    );
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var keyFunction = createMethod(keyType, name + "_k", indent + "    ");
                    var valueFunction = createMethod(valueType, name + "_v", indent + "    ");

                    return Tuple.Create(
                        "a{" + keyFunction.Item1 + valueFunction.Item1 + "}",
                        "global::Dbus.Decoder.GetDictionary(" + body + ", ref " + index + ", " + keyFunction.Item2 + ", " + valueFunction.Item2 + ")"
                    );
                }
                else
                    throw new InvalidOperationException("Only IEnumerable and IDictionary are supported as generic type");
            }

        }

        private static Tuple<string, string> createMethod(Type type, string name, string indent)
        {
            if (SignatureString.For.ContainsKey(type))
                return Tuple.Create(
                    SignatureString.For[type],
                    "global::Dbus.Decoder.Get" + type.Name
                );
            else
            {
                var decoder = new DecoderGenerator(name + "_b");
                decoder.add(name + "_inner", type, indent + "    ", name + "_i");
                //var function = decoder(name, type, indent, name + "_b", name + "_i");
                return Tuple.Create(
                    decoder.Signature,
                    "(byte[] " + name + "_b, ref int " + name + @"_i) =>
" + indent + @"{
" + decoder.Result + @"
    " + indent + "return " + name + @"_inner;
" + indent + "}"
                );
            }
        }
    }
}
