using System;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static class EncoderGenerator
    {
        public static (StringBuilder code, string signature) CreateFor(
            Type type,
            string parameterName,
            string parameter,
            string resultParameter
        )
        {
            var code = new StringBuilder();
            var signature = "";
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @")
                {");
                var innerResult = CreateFor(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                code.Append(innerResult.code);
                signature += "a" + innerResult.signature;
                code.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @") 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                var keyStep = dictionaryKeyStep(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                var valueStep = dictionaryValueStep(type.GenericTypeArguments[1], parameterName, parameter + "_e", resultParameter + "_e");
                code.Append(keyStep.code);
                code.Append(valueStep.code);
                signature += "a{" + keyStep.signature + valueStep.signature + "}";
                code.AppendLine(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                signature += SignatureString.For[type];
                code.Append(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ");");
            }
            else
            {
                signature += SignatureString.For[type];
                code.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ");");
            }
            return (code, signature);
        }

        private static (StringBuilder code, string signature) dictionaryKeyStep(
            Type type,
            string parameterName,
            string parameter,
            string resultParameter
        )
        {
            var code = new StringBuilder();
            var signature = "";
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Key) 
                {");
                var innerResult = CreateFor(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                code.Append(innerResult.code);
                signature += "a" + innerResult.signature;
                code.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Key) 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                var keyStep = dictionaryKeyStep(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                var valueStep = dictionaryValueStep(type.GenericTypeArguments[1], parameterName, parameter + "_e", resultParameter + "_e");
                code.Append(keyStep.code);
                code.Append(valueStep.code);
                signature += "a{" + keyStep.signature + valueStep.signature + "}";
                code.Append(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                signature += SignatureString.For[type];
                code.Append(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Key);");
            }
            else
            {
                signature += SignatureString.For[type];
                code.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Key);");
            }

            return (code, signature);
        }

        private static (StringBuilder code, string signature) dictionaryValueStep(
            Type type,
            string parameterName,
            string parameter,
            string resultParameter
        )
        {
            var code = new StringBuilder();
            var signature = "";
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Value) 
                {");
                var innerResult = CreateFor(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                code.Append(innerResult.code);
                signature += "a" + innerResult.signature;
                code.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Value) 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                var keyStep = dictionaryKeyStep(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                var valueStep = dictionaryValueStep(type.GenericTypeArguments[1], parameterName, parameter + "_e", resultParameter + "_e");
                code.Append(keyStep.code);
                code.Append(valueStep.code);
                signature += "a{" + keyStep.signature + valueStep.signature + "}";
                code.Append(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                signature += SignatureString.For[type];
                code.AppendLine(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Value);");
            }
            else
            {
                signature += SignatureString.For[type];
                code.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Value);");
            }
            return (code, signature);
        }
    }
}
