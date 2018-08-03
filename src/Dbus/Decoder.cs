using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Dbus
{
    public class Decoder : IDisposable
    {
        private static readonly Dictionary<string, Func<Decoder, object>> typeDecoders = new Dictionary<string, Func<Decoder, object>>
        {
            ["o"] = d => d.GetObjectPath(),
            ["s"] = d => d.GetString(),
            ["g"] = d => d.GetSignature(),
            ["y"] = d => d.GetByte(),
            ["b"] = d => d.GetBoolean(),
            ["n"] = d => d.GetInt16(),
            ["q"] = d => d.GetUInt16(),
            ["i"] = d => d.GetInt32(),
            ["u"] = d => d.GetUInt32(),
            ["x"] = d => d.GetInt64(),
            ["t"] = d => d.GetUInt64(),
            ["d"] = d => d.GetDouble(),
            ["a{sv}"] = d => d.getPropertyList(),
            ["as"] = d => d.getStringArray(),
        };

        private readonly IMemoryOwner<byte> memoryOwner;
        private readonly int bufferLength;
        private int index;

        public Decoder(IMemoryOwner<byte> memoryOwner, int bufferLength)
        {
            this.memoryOwner = memoryOwner;
            this.bufferLength = bufferLength;
            index = 0;
        }

        public bool IsFinished => index >= bufferLength;
        public void AdvanceToAlignment(int alignment) => Alignment.Advance(ref index, alignment);

        /// <summary>
        /// Decoder for element types
        /// </summary>
        /// <typeparam name="T">Result type of the decoder</typeparam>
        /// <param name="decoder">The decoder</param>
        /// <returns>The decoded type</returns>
        public delegate T ElementDecoder<T>();

        private IDictionary<string, object> getPropertyList() => GetDictionary(GetString, GetObject);

        private List<string> getStringArray() => GetArray(GetString);

        private unsafe string getStringFromBytes(int length)
        {
            var result = string.Empty;
            if (length != 0)
            {
                var bytes = memoryOwner.Memory.Span.Slice(index, length);
                fixed (byte* bytesP = bytes)
                    result = Encoding.UTF8.GetString(bytesP, length);
            }
            index += length + 1 /* null byte */;
            return result;
        }

        /// <summary>
        /// Decodes a string from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded string</returns>
        public string GetString()
        {
            var stringLength = GetInt32(); // Actually uint
            return getStringFromBytes(stringLength);
        }

        /// <summary>
        /// Decodes an object path from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded object path</returns>
        public ObjectPath GetObjectPath() => GetString();

        /// <summary>
        /// Decodes a signature from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded signature</returns>
        public Signature GetSignature()
        {
            var signatureLength = GetByte();
            return getStringFromBytes(signatureLength);
        }

        private T getPrimitive<T>(int shiftWidth) where T : struct
        {
            var alignment = 1 << shiftWidth;
            AdvanceToAlignment(alignment);
            var typedSpan = MemoryMarshal.Cast<byte, T>(memoryOwner.Memory.Span);
            var result = typedSpan[index >> shiftWidth];
            index += alignment;
            return result;
        }

        /// <summary>
        /// Decodes a Byte from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded Byte</returns>
        public byte GetByte() => getPrimitive<byte>(0);

        /// <summary>
        /// Decodes a Boolean from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded Boolean</returns>
        public bool GetBoolean() => getPrimitive<int>(2) != 0;

        /// <summary>
        /// Decodes an Int16 from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded Int16</returns>
        public short GetInt16() => getPrimitive<short>(1);

        /// <summary>
        /// Decodes an UInt16 from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded UInt16</returns>
        public ushort GetUInt16() => getPrimitive<ushort>(1);

        /// <summary>
        /// Decodes an Int32 from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded Int32</returns>
        public int GetInt32() => getPrimitive<int>(2);

        /// <summary>
        /// Decodes an UInt32 from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded UInt32</returns>
        public uint GetUInt32() => getPrimitive<uint>(2);

        /// <summary>
        /// Decodes an Int64 from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded Int64</returns>
        public long GetInt64() => getPrimitive<long>(3);

        /// <summary>
        /// Decodes an UInt64 from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded UInt64</returns>
        public ulong GetUInt64() => getPrimitive<ulong>(3);

        /// <summary>
        /// Decodes an Double from the buffer and advances the index
        /// </summary>
        /// <returns>The decoded Double</returns>
        public double GetDouble() => getPrimitive<double>(3);

        /// <summary>
        /// Decodes an array from the buffer and advances the index
        /// </summary>
        /// <typeparam name="T">Type of the array elements</typeparam>
        /// <param name="decoder">The decoder for the elements</param>
        /// <returns>The decoded array</returns>
        public List<T> GetArray<T>(ElementDecoder<T> decoder)
        {
            var result = new List<T>();
            var arrayLength = GetInt32(); // Actually uint
            var startIndex = index;
            while (index - startIndex < arrayLength)
            {
                var element = decoder();
                result.Add(element);
            }
            return result;
        }

        /// <summary>
        /// Decodes a dictionary from the buffer and advances the index
        /// </summary>
        /// <typeparam name="TKey">Type of the dictionary keys</typeparam>
        /// <typeparam name="TValue">Type of the dictionary values</typeparam>
        /// <param name="keyDecoder">The decoder for the keys</param>
        /// <param name="valueDecoder">The decoder for the values</param>
        /// <returns>The decoded dictionary</returns>
        public IDictionary<TKey, TValue> GetDictionary<TKey, TValue>(
            ElementDecoder<TKey> keyDecoder,
            ElementDecoder<TValue> valueDecoder
        )
        {
            var result = new Dictionary<TKey, TValue>();
            var arrayLength = GetInt32(); // Actually uint
            AdvanceToAlignment(8);
            var startIndex = index;
            while (index - startIndex < arrayLength)
            {
                AdvanceToAlignment(8);

                var key = keyDecoder();
                var value = valueDecoder();

                result.Add(key, value);
            }
            return result;
        }

        public object GetObject()
        {
            var signature = GetSignature();
            var stringSignature = signature.ToString();
            if (typeDecoders.TryGetValue(stringSignature, out var elementDecoder))
                return elementDecoder(this);
            else if (stringSignature.StartsWith("a"))
            {
                if (typeDecoders.TryGetValue(stringSignature.Substring(1), out elementDecoder))
                    return GetArray(() => elementDecoder(this));
            }
            throw new InvalidOperationException($"Variant type isn't implemented: {signature}");
        }

        public void Dispose() => memoryOwner.Dispose();
    }
}
