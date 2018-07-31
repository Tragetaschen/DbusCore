using System;
using System.Buffers;

namespace Dbus
{
    internal static class ExtensionMethods
    {
        public static void Dump(this byte[] buffer)
        {
            for (var i = 0; i < buffer.Length; ++i)
            {
                var isDigitOrAsciiLetter = false;
                isDigitOrAsciiLetter |= 48 <= buffer[i] && buffer[i] <= 57;
                isDigitOrAsciiLetter |= 65 <= buffer[i] && buffer[i] <= 90;
                isDigitOrAsciiLetter |= 97 <= buffer[i] && buffer[i] <= 122;
                if (isDigitOrAsciiLetter)
                    Console.Write($"{(char)buffer[i]} ");
                else
                    Console.Write($"x{buffer[i]:X} ");
            }
            Console.WriteLine();
        }

        public static ReadOnlySpan<byte> Limit(this IMemoryOwner<byte> memoryOwner, int length)
            => memoryOwner.Memory.Span.Slice(0, length);
    }
}
