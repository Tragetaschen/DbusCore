using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dbus
{
    public partial class Connection
    {
        private void receive()
        {
            const int controlLength = 16 * sizeof(int);

            Span<dbusFixedLengthHeader> fixedLengthHeader = stackalloc dbusFixedLengthHeader[1]; // header up until the array length
            Span<byte> control = stackalloc byte[controlLength];

            var hasValidFixedHeader = false;

            while (true)
                handleOneMessage(fixedLengthHeader, control, ref hasValidFixedHeader);
        }

        private void handleOneMessage(Span<dbusFixedLengthHeader> fixedLengthHeaderSpan, Span<byte> control, ref bool hasValidFixedHeader)
        {
            var fixedLengthHeaderBytes = MemoryMarshal.Cast<dbusFixedLengthHeader, byte>(fixedLengthHeaderSpan);

            if (!hasValidFixedHeader)
                socketOperations.ReceiveMessage(
                    fixedLengthHeaderBytes,
                    control
                );

            ref var fixedLengthHeader = ref fixedLengthHeaderSpan[0];

            if (fixedLengthHeader.Endianess != dbusEndianess.LittleEndian)
                throw new InvalidDataException("Wrong endianess");
            if (fixedLengthHeader.ProtocolVersion != 1)
                throw new InvalidDataException("Wrong protocol version");

            // Store values before receiving the next header
            var messageType = fixedLengthHeader.MessageType;
            var serial = fixedLengthHeader.Serial;
            var shouldSendReply = !fixedLengthHeader.Flags.HasFlag(dbusFlags.NoReplyExpected);
            var receivedArrayLength = fixedLengthHeader.ArrayLength;
            Alignment.Advance(ref receivedArrayLength, 8);
            Span<byte> headerBytes = stackalloc byte[receivedArrayLength];
            var bodyBytes = new byte[fixedLengthHeader.BodyLength];

            hasValidFixedHeader = socketOperations.ReceiveMessage(
                headerBytes,
                bodyBytes,
                fixedLengthHeaderBytes,
                control
            );

            var header = new MessageHeader(socketOperations, headerBytes, control);

            switch (messageType)
            {
                case dbusMessageType.MethodCall:
                    handleMethodCall(
                        serial,
                        header,
                        bodyBytes,
                        shouldSendReply
                    );
                    break;
                case dbusMessageType.MethodReturn:
                    handleMethodReturn(header, bodyBytes);
                    break;
                case dbusMessageType.Error:
                    handleError(header, bodyBytes);
                    break;
                case dbusMessageType.Signal:
                    handleSignal(header, bodyBytes);
                    break;
            }
        }

        private struct dbusFixedLengthHeader
        {
            public dbusEndianess Endianess;
            public dbusMessageType MessageType;
            public dbusFlags Flags;
            public byte ProtocolVersion;
            public int BodyLength;
            public uint Serial;
            public int ArrayLength;
        }

        private enum dbusEndianess : byte
        {
            LittleEndian = (byte)'l',
            BigEndian = (byte)'B',
        }

        private enum dbusMessageType : byte
        {
            Invalid,
            MethodCall,
            MethodReturn,
            Error,
            Signal,
        }

        [Flags]
        private enum dbusFlags : byte
        {
            NoReplyExpected = 0x1,
            NoAutoStart = 0x2,
            AllowInteractiveAuthorization = 0x4,
        }
    }
}
