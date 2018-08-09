using System;
using System.Buffers;

namespace Dbus
{
    internal static class ExtensionMethods
    {
        public static void Dump(this ReadOnlySequence<byte> buffers)
        {
            foreach (var segment in buffers)
            {
                var buffer = segment.Span;
                dump(buffer);
            }
            Console.WriteLine();
        }

        public static void Dump(this Span<byte> memory)
        {
            dump(memory);
            Console.WriteLine();
        }

        private static void dump(this ReadOnlySpan<byte> buffer)
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
        }
    }
}
