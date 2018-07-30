using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static System.Buffers.Text.Encodings;

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
            ["t"] = box(GetUInt64),
            ["d"] = box(GetDouble),
            ["a{sv}"] = getPropertyList,
            ["as"] = getStringArray,
        };

        /// <summary>
        /// Decoder for element types
        /// </summary>
        /// <typeparam name="T">Result type of the decoder</typeparam>
        /// <param name="buffer">Buffer to decode from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded type</returns>
        public delegate T ElementDecoder<T>(ReadOnlySpan<byte> buffer, ref int index);

        private static ElementDecoder<object> box<T>(ElementDecoder<T> orig)
            => (ReadOnlySpan<byte> buffer, ref int index) => orig(buffer, ref index);

        private static IDictionary<string, object> getPropertyList(ReadOnlySpan<byte> buffer, ref int index)
            => GetDictionary(buffer, ref index, GetString, GetObject);

        private static List<string> getStringArray(ReadOnlySpan<byte> buffer, ref int index)
            => GetArray(buffer, ref index, GetString);

        /// <summary>
        /// Decodes a string from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the string from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded string</returns>
        public static string GetString(ReadOnlySpan<byte> buffer, ref int index)
        {
            var stringLength = GetInt32(buffer, ref index); // Actually uint
            var stringBytes = buffer.Slice(index, stringLength);
            var result = Utf8.ToString(stringBytes);
            index += stringLength + 1 /* null byte */;
            return result;
        }

        /// <summary>
        /// Decodes an object path from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the object path from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded object path</returns>
        public static ObjectPath GetObjectPath(ReadOnlySpan<byte> buffer, ref int index)
            => GetString(buffer, ref index);

        /// <summary>
        /// Decodes a signature from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the signature from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded signature</returns>
        public static Signature GetSignature(ReadOnlySpan<byte> buffer, ref int index)
        {
            var signatureLength = GetByte(buffer, ref index);
            var signatureBytes = buffer.Slice(index, signatureLength);
            var result = Utf8.ToString(signatureBytes);
            index += signatureLength + 1 /* null byte */;
            return result;
        }

        private static T getPrimitive<T>(ReadOnlySpan<byte> buffer, ref int index) where T : struct
        {
            int alignment;
            int shiftWidth;
            if (typeof(T) == typeof(byte))
            {
                alignment = 1;
                shiftWidth = 0;
            }
            else if (typeof(T) == typeof(short) || typeof(T) == typeof(ushort))
            {
                alignment = 2;
                shiftWidth = 1;
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(uint))
            {
                alignment = 4;
                shiftWidth = 2;
            }
            else if (typeof(T) == typeof(long) || typeof(T) == typeof(ulong) || typeof(T) == typeof(double))
            {
                alignment = 8;
                shiftWidth = 3;
            }
            else
                throw new InvalidOperationException("Unsupported primitive type: " + typeof(T));

            Alignment.Advance(ref index, alignment);
            var typedSpan = MemoryMarshal.Cast<byte, T>(buffer);
            var result = typedSpan[index >> shiftWidth];
            index += alignment;
            return result;
        }

        /// <summary>
        /// Decodes a Byte from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int32 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Byte</returns>
        public static byte GetByte(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<byte>(buffer, ref index);

        /// <summary>
        /// Decodes a Boolean from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Boolean from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Boolean</returns>
        public static bool GetBoolean(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<int>(buffer, ref index) != 0;

        /// <summary>
        /// Decodes an Int16 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int16 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Int16</returns>
        public static short GetInt16(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<short>(buffer, ref index);

        /// <summary>
        /// Decodes an UInt16 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the UInt16 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded UInt16</returns>
        public static ushort GetUInt16(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<ushort>(buffer, ref index);

        /// <summary>
        /// Decodes an Int32 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int32 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Int32</returns>
        public static int GetInt32(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<int>(buffer, ref index);

        /// <summary>
        /// Decodes an UInt32 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the UInt32 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded UInt32</returns>
        public static uint GetUInt32(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<uint>(buffer, ref index);

        /// <summary>
        /// Decodes an Int64 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Int64 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Int64</returns>
        public static long GetInt64(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<long>(buffer, ref index);

        /// <summary>
        /// Decodes an UInt64 from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the UInt64 from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded UInt64</returns>
        public static ulong GetUInt64(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<ulong>(buffer, ref index);

        /// <summary>
        /// Decodes an Double from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the Double from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded Double</returns>
        public static double GetDouble(ReadOnlySpan<byte> buffer, ref int index)
            => getPrimitive<double>(buffer, ref index);

        /// <summary>
        /// Decodes an array from the buffer and advances the index
        /// </summary>
        /// <typeparam name="T">Type of the array elements</typeparam>
        /// <param name="buffer">Buffer to decode the array from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <param name="decoder">The decoder for the elements</param>
        /// <returns>The decoded array</returns>
        public static List<T> GetArray<T>(ReadOnlySpan<byte> buffer, ref int index, ElementDecoder<T> decoder)
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
            ReadOnlySpan<byte> buffer,
            ref int index,
            ElementDecoder<TKey> keyDecoder,
            ElementDecoder<TValue> valueDecoder
        )
        {
            var result = new Dictionary<TKey, TValue>();
            var arrayLength = GetInt32(buffer, ref index); // Actually uint
            Alignment.Advance(ref index, 8);
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

        public static object GetObject(ReadOnlySpan<byte> buffer, ref int index)
        {
            var signature = GetSignature(buffer, ref index);
            var stringSignature = signature.ToString();
            if (typeDecoders.TryGetValue(stringSignature, out var elementDecoder))
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
