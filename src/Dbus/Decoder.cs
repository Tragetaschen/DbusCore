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
            index += Alignment.Calculate(index, 4);
            var stringLength = BitConverter.ToInt32(buffer, index);
            index += 4;
            var result = Encoding.UTF8.GetString(buffer, index, stringLength);
            index += stringLength + 1 /* null byte */;
            return result;
        }
    }
}
