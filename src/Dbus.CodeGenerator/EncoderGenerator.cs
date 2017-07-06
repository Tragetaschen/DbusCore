﻿using System;
using System.Text;

namespace Dbus.CodeGenerator
{
    public class EncoderGenerator
    {
        public StringBuilder Code { get; } = new StringBuilder();
        public string Signature { get; private set; } = "";

        public void CreateFor(
            Type type,
            string parameterName,
            string parameter,
            string resultParameter
        )
        {
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                Code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @")
                {");
                Signature += "a";
                CreateFor(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                Code.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                Code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @") 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                Signature += "a{";
                dictionaryKeyStep(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                dictionaryValueStep(type.GenericTypeArguments[1], parameterName, parameter + "_e", resultParameter + "_e");
                Signature += "}";
                Code.AppendLine(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                Signature += SignatureString.For[type];
                Code.Append(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ");");
            }
            else
            {
                Signature += SignatureString.For[type];
                Code.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ");");
            }
        }

        private void dictionaryKeyStep(
            Type type,
            string parameterName,
            string parameter,
            string resultParameter
        )
        {
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                Code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Key) 
                {");
                Signature += "a";
                CreateFor(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                Code.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                Code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Key) 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                Signature += "a{";
                dictionaryKeyStep(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                dictionaryValueStep(type.GenericTypeArguments[1], parameterName, parameter + "_e", resultParameter + "_e");
                Signature += "}";
                Code.Append(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                Signature += SignatureString.For[type];
                Code.Append(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Key);");
            }
            else
            {
                Signature += SignatureString.For[type];
                Code.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Key);");
            }
        }

        private void dictionaryValueStep(
            Type type,
            string parameterName,
            string parameter,
            string resultParameter
        )
        {
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                Code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Value) 
                {");
                Signature += "a";
                CreateFor(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                Code.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                Code.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Value) 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                Signature += "a{";
                dictionaryKeyStep(type.GenericTypeArguments[0], parameterName, parameter + "_e", resultParameter + "_e");
                dictionaryValueStep(type.GenericTypeArguments[1], parameterName, parameter + "_e", resultParameter + "_e");
                Signature += "}";
                Code.Append(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                Signature += SignatureString.For[type];
                Code.AppendLine(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Value);");
            }
            else
            {
                Signature += SignatureString.For[type];
                Code.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Value);");
            }
        }
    }
}
