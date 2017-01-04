using System;
using System.Collections.Generic;
using System.Text;

namespace Dbus
{
    public static class Decoder
    {
        /// <summary>
        /// Decodes a string from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the string from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded string</returns>
        public static string GetString(byte[] buffer, ref int index)
        {
            var stringLength = GetInt32(buffer, ref index);
            var result = Encoding.UTF8.GetString(buffer, index, stringLength);
            index += stringLength + 1 /* null byte */;
            return result;
        }

        /// <summary>
        /// Decodes a signature from the buffer and advances the index
        /// </summary>
        /// <param name="buffer">Buffer to decode the signature from</param>
        /// <param name="index">Index into the buffer to start decoding</param>
        /// <returns>The decoded signature</returns>
        public static string GetSignature(byte[] buffer, ref int index)
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
            var arrayLength = GetInt32(buffer, ref index);
            while (index < arrayLength)
            {
                var element = decoder(buffer, ref index);
                result.Add(element);
            }
            return result;
        }
    }
}
