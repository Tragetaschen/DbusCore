﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Dbus;

public sealed class Decoder(MessageHeader? header, IMemoryOwner<byte> memoryOwner, int bufferLength) : IDisposable
{
    private int index = 0;

    /// <summary>
    /// Decodes a Byte from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<byte> GetByte = static Decoder => Decoder.getPrimitive<byte>(0);

    /// <summary>
    /// Decodes a Boolean from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<bool> GetBoolean = static decoder => decoder.getPrimitive<int>(2) != 0;

    /// <summary>
    /// Decodes an Int16 from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<short> GetInt16 = static decoder => decoder.getPrimitive<short>(1);

    /// <summary>
    /// Decodes an UInt16 from the buffer and advances the index
    /// </summary>
    /// <returns>The decoded UInt16</returns>
    public static readonly ElementDecoder<ushort> GetUInt16 = static decoder => decoder.getPrimitive<ushort>(1);

    /// <summary>
    /// Decodes an Int32 from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<int> GetInt32 = static decoder => decoder.getPrimitive<int>(2);

    /// <summary>
    /// Decodes an UInt32 from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<uint> GetUInt32 = static decoder => decoder.getPrimitive<uint>(2);

    /// <summary>
    /// Decodes an Int64 from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<long> GetInt64 = static decoder => decoder.getPrimitive<long>(3);

    /// <summary>
    /// Decodes an UInt64 from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<ulong> GetUInt64 = static decoder => decoder.getPrimitive<ulong>(3);

    /// <summary>
    /// Decodes an Double from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<double> GetDouble = static decoder => decoder.getPrimitive<double>(3);

    /// <summary>
    /// Decodes a signature from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<Signature> GetSignature = static decoder =>
    {
        var signatureLength = GetByte(decoder);
        return decoder.getStringFromBytes(signatureLength);
    };

    /// <summary>
    /// Decodes a file descriptor as SafeHandle and advances the index
    /// </summary>
    public static readonly ElementDecoder<SafeFileHandle> GetSafeHandle = static decoder => decoder.getSafeFileHandle();

    /// <summary>
    /// Decodes a file descriptor as Stream and advances the index
    /// </summary>
    public static readonly ElementDecoder<Stream> GetStream = static decoder =>
    {
        var safeHandle = GetSafeHandle(decoder);
        return new FileStream(safeHandle, FileAccess.ReadWrite);
    };

    /// <summary>
    /// Decodes a string from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<string> GetString = static decoder =>
    {
        var stringLength = GetInt32(decoder); // Actually uint
        return decoder.getStringFromBytes(stringLength);
    };

    /// <summary>
    /// Decodes an object path from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<ObjectPath> GetObjectPath = static decoder => GetString(decoder);

    /// <summary>
    /// Decodes a variant from the buffer and advances the index
    /// </summary>
    public static readonly ElementDecoder<object> GetObject = static decoder =>
    {
        var signature = GetSignature(decoder);
        return DecodeVariant(decoder, signature);
    };

    private static readonly Dictionary<char, (Func<Decoder, object> Decoder, Type Type)> typeDecoders = new()
    {
        ['o'] = (static d => GetObjectPath(d), typeof(ObjectPath)),
        ['s'] = (static d => GetString(d), typeof(string)),
        ['g'] = (static d => GetSignature(d), typeof(Signature)),
        ['y'] = (static d => GetByte(d), typeof(byte)),
        ['b'] = (static d => GetBoolean(d), typeof(bool)),
        ['n'] = (static d => GetInt16(d), typeof(short)),
        ['q'] = (static d => GetUInt16(d), typeof(ushort)),
        ['i'] = (static d => GetInt32(d), typeof(int)),
        ['u'] = (static d => GetUInt32(d), typeof(uint)),
        ['x'] = (static d => GetInt64(d), typeof(long)),
        ['t'] = (static d => GetUInt64(d), typeof(ulong)),
        ['d'] = (static d => GetDouble(d), typeof(double)),
        ['v'] = (static d => GetObject(d), typeof(object)),
    };

    public void AssertSignature(Signature expectedSignature)
    {
        var bodySignature = header?.BodySignature ?? throw new InvalidOperationException("No header or body signature");
        bodySignature.AssertEqual(expectedSignature);
    }

    public void Dump()
        => memoryOwner.Memory.Span[..bufferLength].Dump();

    public bool IsFinished => index >= bufferLength;
    public static void AdvanceToCompoundValue(Decoder decoder) => decoder.advanceToAlignment(8);

    public void Reset() => index = 0;

    private void advanceToAlignment(int alignment) => Alignment.Advance(ref index, alignment);

    /// <summary>
    /// Decoder for element types
    /// </summary>
    /// <typeparam name="T">Result type of the decoder</typeparam>
    /// <param name="decoder">The decoder</param>
    /// <returns>The decoded type</returns>
    public delegate T ElementDecoder<T>(Decoder decoder);

    private string getStringFromBytes(int length)
    {
        var result = string.Empty;
        if (length != 0)
        {
            var bytes = memoryOwner.Memory.Span[index..(index + length)];
            result = Encoding.UTF8.GetString(bytes);
        }
        index += length + 1 /* null byte */;
        return result;
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

    private SafeFileHandle getSafeFileHandle()
    {
        if (header is null)
            throw new InvalidOperationException("Decoder does not support file descriptors");
        var unixFds = header.UnixFds ?? throw new InvalidOperationException("No file descriptors received");
        var index = GetInt32(this);
        return unixFds[index];
    }

    /// <summary>
    /// Decodes an array from the buffer and advances the index
    /// </summary>
    /// <typeparam name="T">Type of the array elements</typeparam>
    /// <param name="decoder">The decoder for the elements</param>
    /// <returns>The decoded array</returns>
    public static List<T> GetArray<T>(Decoder decoder, ElementDecoder<T> elementDecoder, bool storesCompoundValues)
    {
        var result = new List<T>();
        var arrayLength = GetInt32(decoder); // Actually uint
        if (storesCompoundValues)
            AdvanceToCompoundValue(decoder);
        var startIndex = decoder.index;
        while (decoder.index - startIndex < arrayLength)
        {
            var element = elementDecoder(decoder);
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
    public static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(
        Decoder decoder,
        ElementDecoder<TKey> keyDecoder,
        ElementDecoder<TValue> valueDecoder
    ) where TKey : notnull
    {
        var result = new Dictionary<TKey, TValue>();
        var arrayLength = GetInt32(decoder); // Actually uint
        AdvanceToCompoundValue(decoder);
        var startIndex = decoder.index;
        while (decoder.index - startIndex < arrayLength)
        {
            AdvanceToCompoundValue(decoder);

            var key = keyDecoder(decoder);
            var value = valueDecoder(decoder);
            result.Add(key, value);
        }
        return result;
    }

    internal static object DecodeVariant(Decoder decoder, Signature signature)
    {
        var stringSignature = signature.ToString();
        var consumed = 0;
        var decoderInfo = createDecoder(stringSignature, ref consumed);
        if (consumed != stringSignature.Length)
            throw new InvalidOperationException($"Signature '{stringSignature}' was only parsed until index {consumed}");
        return decoderInfo.Decode(decoder);
    }


    private static (ElementDecoder<object>, Type, bool) createArrayDecoder(string signature, ref int consumed)
    {
        var (elementDecoder, elementType, isCompoundType) = createDecoder(signature, ref consumed);
        var arrayType = typeof(List<>).MakeGenericType(elementType);

        object decodeArray(Decoder decoder)
        {
            // See GetArray(…)
            var result = (IList)Activator.CreateInstance(arrayType)!;
            var arrayLength = GetInt32(decoder); // Actually uint
            if (isCompoundType)
                AdvanceToCompoundValue(decoder);
            var startIndex = decoder.index;
            while (decoder.index - startIndex < arrayLength)
            {
                var element = elementDecoder(decoder);
                result.Add(element);
            }
            return result;
        }

        return (decodeArray, arrayType, false);
    }

    private static (ElementDecoder<object>, Type, bool) createDictionaryDecoder(
        string signature,
        ref int consumed
    )
    {
        var (keyDecoder, keyType, _) = createDecoder(signature, ref consumed);
        var (valueDecoder, valueType, _) = createDecoder(signature, ref consumed);
        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);

        object decodeDictionary(Decoder decoder)
        {
            // See GetDictionary(…)
            var result = (IDictionary)Activator.CreateInstance(dictionaryType)!;
            var arrayLength = GetInt32(decoder); // Actually uint
            AdvanceToCompoundValue(decoder);
            var startIndex = decoder.index;
            while (decoder.index - startIndex < arrayLength)
            {
                AdvanceToCompoundValue(decoder);

                var key = keyDecoder(decoder);
                var value = valueDecoder(decoder);
                result.Add(key, value);
            }
            return result;
        }

        return (decodeDictionary, dictionaryType, false);
    }

    private static (ElementDecoder<object>, Type, bool) createTupleDecoder(string signature, ref int consumed)
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

        object decodeTuple(Decoder decoder)
        {
            AdvanceToCompoundValue(decoder);
            var parameters = new object[tupleTypes.Count];
            for (var i = 0; i < tupleTypes.Count; ++i)
                parameters[i] = tupleTypes[i].Decode(decoder);
            return factoryMethod.Invoke(null, parameters)!;
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

        var tupleTypeName = typeof(Tuple).AssemblyQualifiedName!;
        var indexOfFirstComma = tupleTypeName.IndexOf(',');
        var genericTupleTypeName =
            tupleTypeName[..indexOfFirstComma]
            + "`" + types.Length
            + tupleTypeName[indexOfFirstComma..]
        ;
        var tupleType = Type.GetType(genericTupleTypeName)!.MakeGenericType(types);

        return (factoryMethod, tupleType);
    }

    private static (ElementDecoder<object> Decode, Type Type, bool isCompoundValue) createDecoder(
        string signature,
        ref int consumed
    )
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
            return (decoder => typeInfo.Decoder(decoder), typeInfo.Type, false);
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
