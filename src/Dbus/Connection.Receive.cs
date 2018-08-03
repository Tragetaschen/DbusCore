using System;
using System.Buffers;
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
            if (fixedLengthHeader.ProtocolVersion != dbusProtocolVersion.Default)
                throw new InvalidDataException("Wrong protocol version");

            // Store values before receiving the next header
            var messageType = fixedLengthHeader.MessageType;
            var serial = fixedLengthHeader.Serial;
            var shouldSendReply = !fixedLengthHeader.Flags.HasFlag(dbusFlags.NoReplyExpected);
            var bodyLength = fixedLengthHeader.BodyLength;

            var bodyMemoryOwner = MemoryPool<byte>.Shared.Rent(bodyLength);
            var bodyBytes = bodyMemoryOwner.Memory.Span.Slice(0, bodyLength);

            var header = receiveHeaderAndBody(
                ref hasValidFixedHeader,
                fixedLengthHeader.ArrayLength,
                bodyBytes,
                fixedLengthHeaderBytes,
                control
            );

            var decoder = new Decoder(bodyMemoryOwner, bodyLength);

            switch (messageType)
            {
                case dbusMessageType.MethodCall:
                    var methodCallOptions = new MethodCallOptions(header, shouldSendReply);
                    var receivedMessage = new ReceivedMessage(header, decoder);
                    handleMethodCall(
                        methodCallOptions,
                        receivedMessage
                    );
                    break;
                case dbusMessageType.MethodReturn:
                    handleMethodReturn(header, decoder);
                    break;
                case dbusMessageType.Error:
                    handleError(header, decoder);
                    break;
                case dbusMessageType.Signal:
                    handleSignal(header, decoder);
                    break;
            }
        }

        private MessageHeader receiveHeaderAndBody(
            ref bool hasValidFixedHeader,
            int receivedArrayLength,
            Span<byte> bodyBytes,
            Span<byte> fixedLengthHeaderBytes,
            Span<byte> control
        )
        {
            Alignment.Advance(ref receivedArrayLength, 8);
            var headerBytesOwnedMemory = MemoryPool<byte>.Shared.Rent(receivedArrayLength);
            var headerBytes = headerBytesOwnedMemory.Memory.Span.Slice(0, receivedArrayLength);

            hasValidFixedHeader = socketOperations.ReceiveMessage(
                headerBytes,
                bodyBytes,
                fixedLengthHeaderBytes,
                control
            );

            var decoder = new Decoder(headerBytesOwnedMemory, receivedArrayLength);
            try
            {
                return new MessageHeader(socketOperations, decoder, control);
            }
            finally
            {
                headerBytesOwnedMemory.Dispose();
            }
        }

        private struct dbusFixedLengthHeader
        {
            public dbusEndianess Endianess;
            public dbusMessageType MessageType;
            public dbusFlags Flags;
            public dbusProtocolVersion ProtocolVersion;
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
            None = 0,
            NoReplyExpected = 0x1,
            NoAutoStart = 0x2,
            AllowInteractiveAuthorization = 0x4,
        }

        private enum dbusProtocolVersion : byte
        {
            Default = 1,
        }
    }
}
