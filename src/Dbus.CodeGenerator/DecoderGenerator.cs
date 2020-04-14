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
            string signature,
            IEnumerable<string> delegates,
            string delegateName,
            bool isCompoundValue
        )
        {
            Signature = signature;
            Delegates = delegates;
            DelegateName = delegateName;
            IsCompoundValue = isCompoundValue;
        }

        public string Signature { get; }
        public IEnumerable<string> Delegates { get; }
        public string DelegateName { get; }
        public bool IsCompoundValue { get; }

        public static DecoderGenerator Empty() => new DecoderGenerator(
            "",
            new List<string>(),
            "",
            false
        );

        public static DecoderGenerator Create(string name, Type type)
        {
            if (!type.IsConstructedGenericType)
            {
                if (SignatureString.For.TryGetValue(type, out var signature))
                    return new DecoderGenerator(
                        signature,
                        Enumerable.Empty<string>(),
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
                    var delegates = new List<string>(elementDecoder.Delegates)
                    {
                        @"
        private static readonly global::Dbus.Decoder.ElementDecoder<" + Generator.BuildTypeString(type) + "> decode_" + name + @" = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetArray(decoder, " + elementDecoder.DelegateName + ", " + (elementDecoder.IsCompoundValue ? "true" : "false") + @");
"
                    };
                    return new DecoderGenerator(
                        "a" + elementDecoder.Signature,
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

                    var functions = new List<string>();
                    functions.AddRange(keyDecoder.Delegates);
                    functions.AddRange(valueDecoder.Delegates);
                    functions.Add(@"
        private static readonly global::Dbus.Decoder.ElementDecoder<" + Generator.BuildTypeString(type) + "> decode_" + name + @" = (global::Dbus.Decoder decoder)
            => global::Dbus.Decoder.GetDictionary(decoder, " + keyDecoder.DelegateName + ", " + valueDecoder.DelegateName + @");
");

                    return new DecoderGenerator(
                        "a{" + keyDecoder.Signature + valueDecoder.Signature + "}",
                        functions,
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
            var constructorParameters = type.GetTypeInfo()
                .GetConstructors()
                .Select(x => x.GetParameters())
                .OrderByDescending(x => x.Length)
                .First()
            ;
            var isStruct = type.GetCustomAttribute<NoDbusStructAttribute>() == null;
            var delegates = new List<string>();
            var delegateBuilder = new StringBuilder();
            var signatureBuilder = new StringBuilder();

            delegateBuilder.Append(@"
        private static readonly global::Dbus.Decoder.ElementDecoder<" + Generator.BuildTypeString(type) + "> decode_" + name + @" = (global::Dbus.Decoder decoder) =>
        {");

            if (isStruct)
            {
                delegateBuilder.Append(@"
            global::Dbus.Decoder.AdvanceToCompoundValue(decoder);
");
                signatureBuilder.Append("(");
            }

            foreach (var p in constructorParameters)
            {
                var parameterDecoder = Create(name + "_" + p.Name, p.ParameterType);
                signatureBuilder.Append(parameterDecoder.Signature);
                delegates.AddRange(parameterDecoder.Delegates);
                delegateBuilder.Append(@"
            var " + p.Name + " = " + parameterDecoder.DelegateName + "(decoder);");
            }

            if (isStruct)
                signatureBuilder.Append(")");

            delegateBuilder.Append(@"
            return new " + Generator.BuildTypeString(type) + "(" + string.Join(", ", constructorParameters.Select(x => x.Name)) + @");
        };
");

            delegates.Add(delegateBuilder.ToString());
            return new DecoderGenerator(
                signatureBuilder.ToString(),
                delegates,
                "decode_" + name,
                true
            );
        }
    }
}
