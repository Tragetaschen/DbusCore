using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dbus.CodeGenerator
{
    public class DecoderGenerator
    {
        private DecoderGenerator(
            StringBuilder signature,
            StringBuilder delegates,
            string delegateName,
            bool isCompoundValue
        )
        {
            Signature = signature;
            Delegates = delegates;
            DelegateName = delegateName;
            IsCompoundValue = isCompoundValue;
        }

        public StringBuilder Signature { get; }
        public StringBuilder Delegates { get; }
        public string DelegateName { get; }
        public bool IsCompoundValue { get; }

        public static DecoderGenerator Empty() => new DecoderGenerator(
            new StringBuilder(),
            new StringBuilder(),
            "",
            false
        );

        public static DecoderGenerator Create(string name, Type type)
        {
            if (!type.IsConstructedGenericType)
            {
                if (SignatureString.For.TryGetValue(type, out var simpleSignature))
                    return new DecoderGenerator(
                        new StringBuilder(simpleSignature),
                        new StringBuilder(),
                        "global::Dbus.Decoder.Get" + type.Name,
                        false
                    );
                else
                    return buildFromConstructor(name, type);
            }
            else
            {
                var genericType = type.GetGenericTypeDefinition();
                if (genericType == typeof(IEnumerable<>) || genericType == typeof(IList<>))
                {
                    var elementType = type.GenericTypeArguments[0];
                    var elementDecoder = Create(name + "_e", elementType);
                    var delegates = new StringBuilder();
                    delegates.Append(elementDecoder.Delegates)
                        .Append(@"
        private static readonly global::Dbus.Decoder.ElementDecoder<")
                        .Append(Generator.BuildTypeString(type))
                        .Append("> decode_")
                        .Append(name)
                        .Append(@" = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetArray(decoder, ")
                        .Append(elementDecoder.DelegateName)
                        .Append(", ")
                        .Append(elementDecoder.IsCompoundValue ? "true" : "false")
                        .AppendLine(");")
                    ;
                    var signature = new StringBuilder()
                        .Append("a")
                        .Append(elementDecoder.Signature)
                    ;
                    return new DecoderGenerator(
                        signature,
                        delegates,
                        "decode_" + name,
                        false
                    );
                }
                else if (genericType == typeof(IDictionary<,>))
                {
                    var keyType = type.GenericTypeArguments[0];
                    var valueType = type.GenericTypeArguments[1];
                    var keyDecoder = Create(name + "_k", keyType);
                    var valueDecoder = Create(name + "_v", valueType);

                    var delegates = new StringBuilder();
                    delegates.Append(keyDecoder.Delegates)
                        .Append(valueDecoder.Delegates)
                        .Append(@"
        private static readonly global::Dbus.Decoder.ElementDecoder<")
                        .Append(Generator.BuildTypeString(type))
                        .Append("> decode_")
                        .Append(name)
                        .Append(@" = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, ")
                        .Append(keyDecoder.DelegateName)
                        .Append(", ")
                        .Append(valueDecoder.DelegateName)
                        .AppendLine(");")
                    ;
                    var signature = new StringBuilder()
                        .Append("a{")
                        .Append(keyDecoder.Signature)
                        .Append(valueDecoder.Signature)
                        .Append("}")
                    ;
                    return new DecoderGenerator(
                        signature,
                        delegates,
                        "decode_" + name,
                        false
                    );
                }
                else
                    throw new InvalidOperationException("Only IEnumerable, IList and IDictionary are supported as generic type");
            }
        }

        private static DecoderGenerator buildFromConstructor(string name, Type type)
        {
            var constructorParameters = type
                .GetConstructors()
                .Select(x => x.GetParameters())
                .OrderByDescending(x => x.Length)
                .First()
            ;
            var isStruct = type.GetCustomAttribute<NoDbusStructAttribute>() == null;
            var delegates = new StringBuilder();
            var delegateBuilder = new StringBuilder();
            var signatureBuilder = new StringBuilder();

            delegateBuilder.Append(@"
        private static readonly global::Dbus.Decoder.ElementDecoder<")
                .Append(Generator.BuildTypeString(type))
                .Append("> decode_")
                .Append(name)
                .Append(@" = (global::Dbus.Decoder decoder) =>
        {");

            if (isStruct)
            {
                delegateBuilder.Append(@"
            global::Dbus.Decoder.AdvanceToCompoundValue(decoder);");
                signatureBuilder.Append("(");
            }

            foreach (var p in constructorParameters)
            {
                var parameterDecoder = Create(name + "_" + p.Name, p.ParameterType);
                signatureBuilder.Append(parameterDecoder.Signature);
                delegates.Append(parameterDecoder.Delegates);
                delegateBuilder.Append(@"
            var ")
                    .Append(p.Name)
                    .Append(" = ")
                    .Append(parameterDecoder.DelegateName)
                    .Append("(decoder);")
                ;
            }

            if (isStruct)
                signatureBuilder.Append(")");

            delegateBuilder.Append(@"
            return new ")
                .Append(Generator.BuildTypeString(type))
                .Append("(")
                .AppendJoin(", ", constructorParameters.Select(x => x.Name))
                .AppendLine(@");
        };");

            delegates.Append(delegateBuilder);
            return new DecoderGenerator(
                signatureBuilder,
                delegates,
                "decode_" + name,
                true
            );
        }
    }
}
