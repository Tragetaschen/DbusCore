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

            if (!type.IsConstructedGenericType)
            {
                resultBuilder.Append(Generator.Indent);
                resultBuilder.AppendLine("var " + name + " = global::Dbus.Decoder.Get" + type.Name + "(" + body + ", ref decoderIndex);");

                signatureBuilder.Append(SignatureString.For[type]);
            }
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    resultBuilder.Append(Generator.Indent);
                    resultBuilder.AppendLine("var " + name + " = global::Dbus.Decoder.GetArray(" + body + ", ref decoderIndex, global::Dbus.Decoder.Get" + elementType.Name + ");");

                    signatureBuilder.Append("a");
                    signatureBuilder.Append(SignatureString.For[elementType]);
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];

                    resultBuilder.Append(Generator.Indent);
                    resultBuilder.Append("var result = global::Dbus.Decoder.GetDictionary(" + body + ", ref decoderIndex");
                    resultBuilder.Append(", global::Dbus.Decoder.Get" + keyType.Name);
                    resultBuilder.Append(", global::Dbus.Decoder.Get" + valueType.Name);
                    resultBuilder.AppendLine(");");

                    signatureBuilder.Append("a{");
                    signatureBuilder.Append(SignatureString.For[keyType]);
                    signatureBuilder.Append(SignatureString.For[valueType]);
                    signatureBuilder.Append("}");
                }
                else
                    throw new InvalidOperationException("Only IEnumerable and IDictionary are supported as generic type");
            }
        }
    }
}
