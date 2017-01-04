using System;
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
    }
}
