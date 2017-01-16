using System;
using System.Collections.Generic;
using System.Text;

namespace Dbus
{
    public static class Decoder
    {
        private static readonly Dictionary<string, ElementDecoder<object>> typeDecoders = new Dictionary<string, ElementDecoder<object>>
        {
            ["o"] = GetObjectPath,
            ["s"] = GetString,
            ["g"] = GetSignature,
            ["y"] = box(GetByte),
            ["b"] = box(GetBoolean),
            ["n"] = box(GetInt16),
            ["q"] = box(GetUInt16),
            ["i"] = box(GetInt32),
            ["u"] = box(GetUInt32),
            ["x"] = box(GetInt64),
            ["d"] = box(GetDouble),
            ["a{sv}"] = getPropertyList,
            ["as"] = getStringArray,
        };

        private static ElementDecoder<object> box<T>(ElementDecoder<T> orig)
        {
            return (byte[] buffer, ref int index) => orig(buffer, ref index);
        }

        private static IDictionary<string, object> getPropertyList(byte[] buffer, ref int index)
        {
            return GetDictionary(buffer, ref index, GetString, GetObject);
        }

        private static List<string> getStringArray(byte[] buffer, ref int index)
        {
            return GetArray(buffer, ref index, GetString);
        }

        /// <summary>
        /// Decodes a string from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the string from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded string</returns>
        public static string GetString(byte[] buffer, ref int index)
        {
            var stringLength = GetInt32(buffer, ref index); // Actually uint
            var result = Encoding.UTF8.GetString(buffer, index, stringLength);
            index += stringLength + 1 /* null byte */;
            return result;
        }

        /// <summary>
        /// Decodes an object path from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the object path from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded object path</returns>
        public static ObjectPath GetObjectPath(byte[] buffer, ref int index)
        {
            return GetString(buffer, ref index);
        }

        /// <summary>
        /// Decodes a signature from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the signature from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded signature</returns>
        public static Signature GetSignature(byte[] buffer, ref int index)
        {
            var signatureLength = GetByte(buffer, ref index);
            var result = Encoding.UTF8.GetString(buffer, index, signatureLength);
            index += signatureLength + 1 /* null byte */;
            return result;
        }

        /// <summary>
        /// Decodes a Byte from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int32 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Byte</returns>
        public static byte GetByte(byte[] buffer, ref int index)
        {
            var result = buffer[index];
            index += 1;
            return result;
        }

        /// <summary>
        /// Decodes a Boolean from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Boolean from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Boolean</returns>
        public static bool GetBoolean(byte[] buffer, ref int index)
        {
            return GetInt32(buffer, ref index) != 0;
        }

        /// <summary>
        /// Decodes an Int16 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int16 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Int16</returns>
        public static short GetInt16(byte[] buffer, ref int index)
        {
            Alignment.Advance(ref index, 2);
            var result = BitConverter.ToInt16(buffer, index);
            index += 2;
            return result;
        }

        /// <summary>
        /// Decodes an UInt16 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the UInt16 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded UInt16</returns>
        public static ushort GetUInt16(byte[] buffer, ref int index)
        {
            Alignment.Advance(ref index, 2);
            var result = BitConverter.ToUInt16(buffer, index);
            index += 2;
            return result;
        }

        /// <summary>
        /// Decodes an Int32 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int32 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Int32</returns>
        public static int GetInt32(byte[] buffer, ref int index)
        {
            Alignment.Advance(ref index, 4);
            var result = BitConverter.ToInt32(buffer, index);
            index += 4;
            return result;
        }

        /// <summary>
        /// Decodes an UInt32 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the UInt32 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded UInt32</returns>
        public static uint GetUInt32(byte[] buffer, ref int index)
        {
            Alignment.Advance(ref index, 4);
            var result = BitConverter.ToUInt32(buffer, index);
            index += 4;
            return result;
        }

        /// <summary>
        /// Decodes an Int64 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int64 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Int64</returns>
        public static long GetInt64(byte[] buffer, ref int index)
        {
            Alignment.Advance(ref index, 8);
            var result = BitConverter.ToInt64(buffer, index);
            index += 8;
            return result;
        }

        /// <summary>
        /// Decodes an Double from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Double from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Double</returns>
        public static double GetDouble(byte[] buffer, ref int index)
        {
            Alignment.Advance(ref index, 8);
            var result = BitConverter.ToDouble(buffer, index);
            index += 8;
            return result;
        }

        /// <summary>
        /// Decoder for element types
        /// </summary>
        /// <typeparam name="T">Result type of the decoder</typeparam>
        /// <param name="buffer">Buffer to decode from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded type</returns>
        public delegate T ElementDecoder<T>(byte[] buffer, ref int index);

        /// <summary>
        /// Decodes an array from the buffer and advances the index
        /// </summary>
        /// <typeparam name="T">Type of the array elements</typeparam>
        /// <param name="buffer">Buffer to decode the array from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <param name="decoder">The decoder for the elements</param>
        /// <returns>The decoded array</returns>
        public static List<T> GetArray<T>(byte[] buffer, ref int index, ElementDecoder<T> decoder)
        {
            var result = new List<T>();
            var arrayLength = GetInt32(buffer, ref index); // Actually uint
            var startIndex = index;
            while (index - startIndex < arrayLength)
            {
                var element = decoder(buffer, ref index);
                result.Add(element);
            }
            return result;
        }

        /// <summary>
        /// Decodes a dictionary from the buffer and advances the index
        /// </summary>
        /// <typeparam name="TKey">Type of the dictionary keys</typeparam>
        /// <typeparam name="TValue">Type of the dictionary values</typeparam>
        /// <param name="buffer">Buffer to decode the dictionary from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <param name="keyDecoder">The decoder for the keys</param>
        /// <param name="valueDecoder">The decoder for the values</param>
        /// <returns>The decoded dictionary</returns>
        public static IDictionary<TKey, TValue> GetDictionary<TKey, TValue>(
            byte[] buffer,
            ref int index,
            ElementDecoder<TKey> keyDecoder,
            ElementDecoder<TValue> valueDecoder
        )
        {
            var result = new Dictionary<TKey, TValue>();
            var arrayLength = GetInt32(buffer, ref index); // Actually uint
            var startIndex = index;
            while (index - startIndex < arrayLength)
            {
                Alignment.Advance(ref index, 8);

                var key = keyDecoder(buffer, ref index);
                var value = valueDecoder(buffer, ref index);

                result.Add(key, value);
            }
            return result;
        }

        public static object GetObject(byte[] buffer, ref int index)
        {
            var signature = GetSignature(buffer, ref index);
            var stringSignature = signature.ToString();
            ElementDecoder<object> elementDecoder;
            if (typeDecoders.TryGetValue(stringSignature, out elementDecoder))
                return elementDecoder(buffer, ref index);
            else if (stringSignature.StartsWith("a"))
            {
                if (typeDecoders.TryGetValue(stringSignature.Substring(1), out elementDecoder))
                    return GetArray(buffer, ref index, elementDecoder);
            }
            throw new InvalidOperationException($"Variant type isn't implemented: {signature}");
        }
    }
}
