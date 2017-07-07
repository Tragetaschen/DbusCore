using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dbus.CodeGenerator
{
    public static partial class Generator
    {
        private static string generatePropertyEncodeImplementation(Type type)
        {
            var propertyEncoder = new StringBuilder();
            propertyEncoder.Append(@"
        public void EncodeProperties(global::System.Collections.Generic.List<byte> sendBody, ref int sendIndex)
        {
            global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e, ref int sendIndex_e) =>
            {");

            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                propertyEncoder.Append(@"
                global::Dbus.Encoder.EnsureAlignment(sendBody_e, ref sendIndex_e, 8);
                global::Dbus.Encoder.Add(sendBody_e, ref sendIndex_e, """ + property.Name + @""");
                Encode" + property.Name + @"(sendBody_e, ref sendIndex_e);");
            }
            propertyEncoder.Append(@"
            }, true);
        }

        public void EncodeProperty(global::System.Collections.Generic.List<byte> sendBody, ref int sendIndex, string propertyName)
        {
            switch (propertyName)
            {");
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                propertyEncoder.Append(@"
                case """ + property.Name + @""":
                    Encode" + property.Name + @"(sendBody, ref sendIndex);
                    break;");
            }
            propertyEncoder.Append(@"
                default:
                    throw new global::Dbus.DbusException(
                        global::Dbus.DbusException.CreateErrorName(""UnknownProperty""),
                        ""No such Property: "" + propertyName
                    );
            }
        }");
            foreach (var property in type.GetTypeInfo().GetProperties())
            {
                propertyEncoder.Append(@"

        private void Encode" + property.Name + @"(global::System.Collections.Generic.List<byte> sendBody, ref int sendIndex)
        {
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, (global::Dbus.Signature)""" + createVariantSignature(property.PropertyType) + @""");");
                if (property.PropertyType.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
                {
                    propertyEncoder.Append(@"
            global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e, ref int sendIndex_e) =>
            {
                foreach(var " + property.Name + @"_e in target." + property.Name + @")
                {");
                    generateEnumerableEncoding(propertyEncoder, property.PropertyType.GenericTypeArguments[0], "_e", property);
                    propertyEncoder.Append(@"
                }
            });");
                }
                else if (property.PropertyType.FullName.StartsWith("System.Collections.Generic.IDictionary"))
                {
                    propertyEncoder.Append(@"
            global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e, ref int sendIndex_e) =>
            {
                foreach (var " + property.Name + @"_e in target." + property.Name + @")
                {
                    global::Dbus.Encoder.EnsureAlignment(sendBody_e, ref sendIndex_e, 8);");
                    generateKeyEncoding(propertyEncoder, property.PropertyType.GenericTypeArguments[0], "_e", property);
                    generateValueEncoding(propertyEncoder, property.PropertyType.GenericTypeArguments[1], "_e", property);
                    propertyEncoder.Append(@"
                }
            });");
                }
                else
                {
                    propertyEncoder.Append(@"
            global::Dbus.Encoder.Add(sendBody, ref sendIndex, target." + property.Name + @");");
                }
                propertyEncoder.Append(@"
        }");
            }
            return propertyEncoder.ToString();
        }

        private static string createVariantSignature(Type type)
        {
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                return "a" + createVariantSignature(type.GenericTypeArguments[0]);
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                return "a{" + createVariantSignature(type.GenericTypeArguments[0]) +
                              createVariantSignature(type.GenericTypeArguments[1]) + "}";
            }
            else
            {
                return SignatureString.For[type];
            }
        }

        private static void generateEnumerableEncoding(StringBuilder propertyEncoder, Type type, string parameter, PropertyInfo property, string propertyParameter = "_e")
        {
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                propertyEncoder.Append(@"global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + @", ref int sendIndex_e" + parameter + @") =>
                    {
                    foreach(var " + property.Name + "_e" + parameter + @" in " + property.Name + parameter + @")
                      {");
                generateEnumerableEncoding(propertyEncoder, type.GenericTypeArguments[0], parameter + "_e", property, propertyParameter + "_e");
                propertyEncoder.Append(@"}});
");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                propertyEncoder.Append(@"global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + @", ref int sendIndex_e" + parameter + @") =>
                    {
                    foreach(var " + property.Name + "_e" + parameter + @" in " + property.Name + parameter + @")
                      {{" +
                  "global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);
");
                generateKeyEncoding(propertyEncoder, type.GenericTypeArguments[0], parameter + "_e", property, propertyParameter + "_e");
                generateValueEncoding(propertyEncoder, type.GenericTypeArguments[1], parameter + "_e", property, propertyParameter + "_e");
                propertyEncoder.Append(@"}});
");
            }
            else
            {
                propertyEncoder.Append(@"
                global::Dbus.Encoder.Add(sendBody" + parameter + @", ref sendIndex" + parameter + @"," + property.Name + propertyParameter + @" );
");
            }
        }

        private static void generateKeyEncoding(StringBuilder propertyEncoder, Type type, string parameter, PropertyInfo property, string propertyParameter = "_e")
        {
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                propertyEncoder.Append(@"global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + @", ref int sendIndex_e" + parameter + @") =>
                    {
                    foreach(var " + property.Name + "_e" + parameter + @" in " + property.Name + parameter + @".Key)
                      {");
                generateEnumerableEncoding(propertyEncoder, type.GenericTypeArguments[0], parameter + "_e", property, propertyParameter + "_e");
                propertyEncoder.Append(@"}});
");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                propertyEncoder.Append(@"global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + @", ref int sendIndex_e" + parameter + @") =>
                    {
                    foreach(var " + property.Name + "_e" + parameter + @" in " + property.Name + parameter + @".Key)
                      {{" +
                  "global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);
");
                generateKeyEncoding(propertyEncoder, type.GenericTypeArguments[0], parameter + "_e", property, propertyParameter + "_e");
                generateValueEncoding(propertyEncoder, type.GenericTypeArguments[1], parameter + "_e", property, propertyParameter + "_e");
                propertyEncoder.Append(@"}});
");
            }
            else
            {
                propertyEncoder.Append(@"
                global::Dbus.Encoder.Add(sendBody" + parameter + @", ref sendIndex" + parameter + @"," + property.Name + propertyParameter + @".Key );
");
            }
        }

        private static void generateValueEncoding(StringBuilder propertyEncoder, Type type, string parameter, PropertyInfo property, string propertyParameter = "_e")
        {
            if (type.FullName.StartsWith("System.Collections.Generic.IEnumerable"))
            {
                propertyEncoder.Append(@"global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + @", ref int sendIndex_e" + parameter + @") =>
                    {
                    foreach(var " + property.Name + "_e" + parameter + @" in " + property.Name + parameter + @".Value)
                      {");
                generateEnumerableEncoding(propertyEncoder, type.GenericTypeArguments[0], parameter + "_e", property, propertyParameter + "_e");
                propertyEncoder.Append(@"}});
");
            }
            else if (type.FullName.StartsWith("System.Collections.Generic.IDictionary"))
            {
                propertyEncoder.Append(@"global::Dbus.Encoder.AddArray(sendBody, ref sendIndex, (global::System.Collections.Generic.List<byte> sendBody_e" + parameter + @", ref int sendIndex_e" + parameter + @") =>
                    {
                    foreach(var " + property.Name + "_e" + parameter + @" in " + property.Name + parameter + @".Value)
                      {{" +
                  "global::Dbus.Encoder.EnsureAlignment(sendBody_e" + parameter + ", ref sendIndex_e" + parameter + @", 8);
");
                generateKeyEncoding(propertyEncoder, type.GenericTypeArguments[0], parameter + "_e", property, propertyParameter + "_e");
                generateValueEncoding(propertyEncoder, type.GenericTypeArguments[1], parameter + "_e", property, propertyParameter + "_e");
                propertyEncoder.Append(@"}});
");
            }
            else
            {
                propertyEncoder.Append(@"
                global::Dbus.Encoder.Add(sendBody" + parameter + @", ref sendIndex" + parameter + @"," + property.Name + propertyParameter + @".Value );
");
            }
        }
    }
}
