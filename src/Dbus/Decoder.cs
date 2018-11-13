using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Dbus
{
    public class Decoder : IDisposable
    {
        private static readonly Dictionary<char, (Func<Decoder, object> Decoder, Type Type)> typeDecoders = new Dictionary<char, (Func<Decoder, object>, Type)>
        {
            ['o'] = (d => d.GetObjectPath(), typeof(ObjectPath)),
            ['s'] = (d => d.GetString(), typeof(string)),
            ['g'] = (d => d.GetSignature(), typeof(Signature)),
            ['y'] = (d => d.GetByte(), typeof(byte)),
            ['b'] = (d => d.GetBoolean(), typeof(bool)),
            ['n'] = (d => d.GetInt16(), typeof(short)),
            ['q'] = (d => d.GetUInt16(), typeof(ushort)),
            ['i'] = (d => d.GetInt32(), typeof(int)),
            ['u'] = (d => d.GetUInt32(), typeof(uint)),
            ['x'] = (d => d.GetInt64(), typeof(long)),
            ['t'] = (d => d.GetUInt64(), typeof(ulong)),
            ['d'] = (d => d.GetDouble(), typeof(double)),
            ['v'] = (d => d.GetObject(), typeof(object)),
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

        public void Dump()
            => memoryOwner.Memory.Span.Slice(0, bufferLength).Dump();

        public bool IsFinished => index >= bufferLength;
        public void AdvanceToCompoundValue() => advanceToAlignment(8);

        private void advanceToAlignment(int alignment) => Alignment.Advance(ref index, alignment);

        /// <summary>
        /// Decoder for element types
        /// </summary>
        /// <typeparam name="T">Result type of the decoder</typeparam>
        /// <param name="decoder">The decoder</param>
        /// <returns>The decoded type</returns>
        public delegate T ElementDecoder<T>();

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
            advanceToAlignment(alignment);
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
        public List<T> GetArray<T>(ElementDecoder<T> decoder, bool storesCompoundValues)
        {
            var result = new List<T>();
            var arrayLength = GetInt32(); // Actually uint
            if (storesCompoundValues)
                AdvanceToCompoundValue();
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
            AdvanceToCompoundValue();
            var startIndex = index;
            while (index - startIndex < arrayLength)
            {
                AdvanceToCompoundValue();

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
            var consumed = 0;
            var decoderInfo = createDecoder(stringSignature, ref consumed);
            if (consumed != stringSignature.Length)
                throw new InvalidOperationException($"Signature '{stringSignature}' was only parsed until index {consumed}");
            return decoderInfo.Decode();
        }

        private (ElementDecoder<object>, Type, bool) createArrayDecoder(string signature, ref int consumed)
        {
            var (elementDecoder, elementType, isCompoundType) = createDecoder(signature, ref consumed);
            var arrayType = typeof(List<>).MakeGenericType(elementType);

            object decodeArray()
            {
                // See GetArray(…)
                var result = (IList)Activator.CreateInstance(arrayType);
                var arrayLength = GetInt32(); // Actually uint
                if (isCompoundType)
                    AdvanceToCompoundValue();
                var startIndex = index;
                while (index - startIndex < arrayLength)
                {
                    var element = elementDecoder();
                    result.Add(element);
                }
                return result;
            }

            return (decodeArray, arrayType, false);
        }

        private (ElementDecoder<object>, Type, bool) createDictionaryDecoder(string signature, ref int consumed)
        {
            var (keyDecoder, keyType, _) = createDecoder(signature, ref consumed);
            var (valueDecoder, valueType, _) = createDecoder(signature, ref consumed);
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

            object decodeDictionary()
            {
                // See GetDictionary(…)
                var result = (IDictionary)Activator.CreateInstance(dictionaryType);
                var arrayLength = GetInt32(); // Actually uint
                AdvanceToCompoundValue();
                var startIndex = index;
                while (index - startIndex < arrayLength)
                {
                    AdvanceToCompoundValue();

                    var key = keyDecoder();
                    var value = valueDecoder();
                    result.Add(key, value);
                }
                return result;
            }

            return (decodeDictionary, dictionaryType, false);
        }

        private (ElementDecoder<object>, Type, bool) createTupleDecoder(string signature, ref int consumed)
        {
            var tupleTypes = new List<(ElementDecoder<object> Decode, Type Type, bool isCompoundValue)>();
            var origConsumed = consumed;

            while (signature[consumed] != ')')
            {
                var tupleTypeDecoder = createDecoder(signature, ref consumed);
                tupleTypes.Add(tupleTypeDecoder);
            }
            var types = tupleTypes.Select(x => x.Type).ToArray();
            if (types.Length >= 8)
                // System.Tuple only exists up until System.Tuple`7
                // System.Tuple`8 explicitly has the documented semantics of using the last type parameter
                // for another System.Tuple, to store more values, but that's not yet supported here
                throw new InvalidOperationException("Structs in variants only support up to 8 elements: " + signature);

            var (factoryMethod, tupleType) = buildTuple(types);

            object decodeTuple()
            {
                AdvanceToCompoundValue();
                var parameters = new object[tupleTypes.Count];
                for (var i = 0; i < tupleTypes.Count; ++i)
                    parameters[i] = tupleTypes[i].Decode();
                return factoryMethod.Invoke(null, parameters);
            }

            return (decodeTuple, tupleType, true);
        }

        private static (MethodInfo, Type) buildTuple(Type[] types)
        {
            var factoryMethod = typeof(Tuple)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(x =>
                    x.Name == "Create" &&
                    x.GetGenericArguments().Length == types.Length
                )
                .Single()
                .MakeGenericMethod(types);

            var tupleTypeName = typeof(Tuple).AssemblyQualifiedName;
            var indexOfFirstComma = tupleTypeName.IndexOf(',');
            var genericTupleTypeName =
                tupleTypeName.Substring(0, indexOfFirstComma)
                + "`" + types.Length
                + tupleTypeName.Substring(indexOfFirstComma)
            ;
            var tupleType = Type.GetType(genericTupleTypeName).MakeGenericType(types);

            return (factoryMethod, tupleType);
        }

        private (ElementDecoder<object> Decode, Type Type, bool isCompoundValue) createDecoder(string signature, ref int consumed)
        {
            if (signature[consumed] == 'a')
                if (signature[consumed + 1] == '{')
                {
                    consumed += 2; // a{
                    var dictionaryResult = createDictionaryDecoder(signature, ref consumed);
                    if (signature[consumed] != '}')
                        throw new InvalidOperationException($"Unbalanced dictionary in variant: '{signature}' at index {consumed}");
                    consumed += 1; // }
                    return dictionaryResult;
                }
                else
                {
                    consumed += 1; // a
                    return createArrayDecoder(signature, ref consumed);
                }
            else if (typeDecoders.TryGetValue(signature[consumed], out var typeInfo))
            {
                consumed += 1;  // the type char
                return (() => typeInfo.Decoder(this), typeInfo.Type, false);
            }
            else if (signature[consumed] == '(')
            {
                consumed += 1; // (
                var tupleResult = createTupleDecoder(signature, ref consumed);
                consumed += 1; // )
                return tupleResult;
            }
            else
                throw new InvalidOperationException($"Unknown type in variant: '{signature}' at index {consumed}");
        }

        public void Dispose() => memoryOwner.Dispose();
    }
}
