using System;
using System.Text;

namespace Dbus.CodeGenerator
{
    public static class EncoderGenerator
    {
        public static string buildSignature(Type type, StringBuilder encoders, string parameterName = "result", string parameter = "", string resultParameter = "", int level = 0)
        {
            string signature = "";
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                encoders.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") =>
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @")
                {");
                signature += "a" + buildSignature(type.GenericTypeArguments[0], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1);
                encoders.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                encoders.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") => 
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @") 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                signature += "a{" + dictionaryKeyStep(type.GenericTypeArguments[0], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1)
                                      + dictionaryValueStep(type.GenericTypeArguments[1], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1)
                                      + "}";
                encoders.AppendLine(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                signature += SignatureString.For[type];
                if (level > 0)
                    encoders.Append(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ");");
                else
                    encoders.Append(@"
            global::Dbus.Encoder.AddVariant(sendBody, ref sendIndex, " + parameterName + ");");
            }
            else
            {
                signature += SignatureString.For[type];
                if (level > 0)
                    encoders.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ");");
                else
                    encoders.Append(@"
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, " + parameterName + ");");
            }
            return signature;
        }

        public static string buildSignature(Type type)
        {
            string signature = "";
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
                signature += "a" + buildSignature(type.GenericTypeArguments[0]);
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
                signature += "a{" + buildSignature(type.GenericTypeArguments[0]) +
                    buildSignature(type.GenericTypeArguments[1]) +
                    "}";
            else
                signature += SignatureString.For[type];
            return signature;
        }

        private static string dictionaryKeyStep(Type type, StringBuilder encoders, string parameterName, string parameter = "", string resultParameter = "", int level = 0)
        {
            string signature = "";
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                encoders.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") => 
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Key) 
                {");
                signature += "a" + buildSignature(type.GenericTypeArguments[0], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1);
                encoders.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                encoders.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") => 
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Key) 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                signature += "a{" + dictionaryKeyStep(type.GenericTypeArguments[0], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1)
                                      + dictionaryValueStep(type.GenericTypeArguments[1], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1)
                                      + "}";
                encoders.Append(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                signature += SignatureString.For[type];
                if (level > 0)
                    encoders.Append(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Key);");
                else
                    encoders.Append(@"
            global::Dbus.Encoder.AddVariant(sendBody, ref sendIndex, " + parameterName + ");");
            }
            else
            {
                signature += SignatureString.For[type];
                if (level > 0)
                    encoders.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Key);");
                else
                    encoders.Append(@"
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, " + parameterName + ");");
            }

            return signature;
        }

        private static string dictionaryValueStep(Type type, StringBuilder encoders, string parameterName, string parameter = "", string resultParameter = "", int level = 0)
        {
            string signature = "";
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                encoders.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") => 
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Value) 
                {");
                signature += "a" + buildSignature(type.GenericTypeArguments[0], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1);
                encoders.Append(@"
                }
            });");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                encoders.AppendLine("global::Dbus.Encoder.AddArray(sendBody" + parameter + ", ref sendIndex" + parameter + ", (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + ", ref int sendIndex_e" + parameter + @") => 
            {
                foreach (var " + parameterName + "_e" + resultParameter + " in " + parameterName + resultParameter + @".Value) 
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);");
                signature += "a{" + dictionaryKeyStep(type.GenericTypeArguments[0], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1)
                                      + dictionaryValueStep(type.GenericTypeArguments[1], encoders, parameterName, parameter + "_e", resultParameter + "_e", level + 1)
                                      + "}";
                encoders.Append(@"
                }
            }, true);");
            }
            else if (type == typeof(object))
            {
                signature += SignatureString.For[type];
                if (level > 0)
                    encoders.AppendLine(@"
                    global::Dbus.Encoder.AddVariant(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Value);");
                else
                    encoders.AppendLine(@"
            global::Dbus.Encoder.AddVariant(sendBody, ref sendIndex, " + parameterName + ");");
            }
            else
            {
                signature += SignatureString.For[type];
                if (level > 0)
                    encoders.Append(@"
                    global::Dbus.Encoder.Add(sendBody" + parameter + ", ref sendIndex" + parameter + ", " + parameterName + resultParameter + ".Value);");
                else
                    encoders.Append(@"
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, " + parameterName + ");");
            }
            return signature;
        }
    }
}
