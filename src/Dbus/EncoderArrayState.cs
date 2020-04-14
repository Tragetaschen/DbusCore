using System;

namespace Dbus
{
    public readonly ref struct EncoderArrayState
    {
        public EncoderArrayState(int arrayStart, Span<byte> lengthSpan)
        {
            ArrayStart = arrayStart;
            LengthSpan = lengthSpan;
        }

        public int ArrayStart { get; }
        public Span<byte> LengthSpan { get; }
    }
}
