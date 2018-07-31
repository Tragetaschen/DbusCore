using System;
using System.IO;

namespace Dbus
{
    public partial class Connection
    {
        private void receive()
        {
            const int fixedLengthHeaderLength = 16;
            const int controlLength = 16 * sizeof(int);

            Span<byte> fixedLengthHeader = stackalloc byte[fixedLengthHeaderLength]; // header up until the array length
            Span<byte> control = stackalloc byte[controlLength];

            var hasValidFixedHeader = false;

            while (true)
                handleOneMessage(fixedLengthHeader, control, ref hasValidFixedHeader);
        }

        private void handleOneMessage(Span<byte> fixedLengthHeader, Span<byte> control, ref bool hasValidFixedHeader)
        {
            if (!hasValidFixedHeader)
                socketOperations.ReceiveMessage(
                    fixedLengthHeader,
                    control
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
            Span<byte> headerBytes = stackalloc byte[receivedArrayLength];
            var bodyBytes = new byte[bodyLength];

            hasValidFixedHeader = socketOperations.ReceiveMessage(
                headerBytes,
                bodyBytes,
                fixedLengthHeader,
                control
            );

            var header = new MessageHeader(socketOperations, headerBytes, control);

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
