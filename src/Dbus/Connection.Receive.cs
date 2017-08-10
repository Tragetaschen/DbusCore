using System;
using System.IO;

namespace Dbus
{
    public partial class Connection
    {
        private unsafe void receive()
        {
            const int fixedLengthHeaderLength = 16;
            const int controlLength = 16;

            var fixedLengthHeader = new byte[fixedLengthHeaderLength]; // header up until the array length
            var control = stackalloc int[controlLength];

            var hasValidFixedHeader = false;

            while (true)
            {
                if (!hasValidFixedHeader)
                    fixed (byte* fixedLengthHeaderP = fixedLengthHeader)
                        socketOperations.ReceiveMessage(
                            fixedLengthHeaderP, fixedLengthHeaderLength,
                            control, controlLength
                        );

                var index = 0;
                var endianess = Decoder.GetByte(fixedLengthHeader, ref index);
                if (endianess != (byte)'l')
                    throw new InvalidDataException("Wrong endianess");
                var messageType = Decoder.GetByte(fixedLengthHeader, ref index);
                var shouldSendReply = (Decoder.GetByte(fixedLengthHeader, ref index) & 0x1) == 0x0;
                var protocolVersion = Decoder.GetByte(fixedLengthHeader, ref index);
                if (protocolVersion != 1)
                    throw new InvalidDataException("Wrong protocol version");
                var bodyLength = Decoder.GetInt32(fixedLengthHeader, ref index); // Actually uint
                var receivedSerial = Decoder.GetUInt32(fixedLengthHeader, ref index);
                var receivedArrayLength = Decoder.GetInt32(fixedLengthHeader, ref index); // Actually uint
                Alignment.Advance(ref receivedArrayLength, 8);
                var headerBytes = new byte[receivedArrayLength];
                var bodyBytes = new byte[bodyLength];

                fixed (byte* headerP = headerBytes)
                fixed (byte* bodyP = bodyBytes)
                fixed (byte* fixedLengthHeaderP = fixedLengthHeader)
                    hasValidFixedHeader = socketOperations.ReceiveMessage(
                        headerP, receivedArrayLength,
                        bodyP, bodyLength,
                        fixedLengthHeaderP, fixedLengthHeaderLength,
                        control, controlLength
                    );

                var header = new MessageHeader(headerBytes, control);

                switch (messageType)
                {
                    case 1:
                        handleMethodCall(receivedSerial, header, bodyBytes, shouldSendReply);
                        break;
                    case 2:
                        handleMethodReturn(header, bodyBytes);
                        break;
                    case 3:
                        handleError(header, bodyBytes);
                        break;
                    case 4:
                        handleSignal(header, bodyBytes);
                        break;
                }
            }
        }
    }
}
