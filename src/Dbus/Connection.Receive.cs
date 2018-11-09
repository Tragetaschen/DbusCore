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

            Span<DbusFixedLengthHeader> fixedLengthHeader = stackalloc DbusFixedLengthHeader[1]; // header up until the array length
            Span<byte> control = stackalloc byte[controlLength];

            var hasValidFixedHeader = false;

            while (true)
                try
                {
                    handleOneMessage(fixedLengthHeader, control, ref hasValidFixedHeader);
                }
                catch (Exception e)
                {
                    foreach (var expectedMessage in expectedMessages)
                        expectedMessage.Value.SetException(e);
                    return;
                }
        }

        private void handleOneMessage(Span<DbusFixedLengthHeader> fixedLengthHeaderSpan, Span<byte> control, ref bool hasValidFixedHeader)
        {
            var fixedLengthHeaderBytes = MemoryMarshal.Cast<DbusFixedLengthHeader, byte>(fixedLengthHeaderSpan);

            if (!hasValidFixedHeader)
                socketOperations.ReceiveMessage(
                    fixedLengthHeaderBytes,
                    control
                );

            ref var fixedLengthHeader = ref fixedLengthHeaderSpan[0];

            if (fixedLengthHeader.Endianess != DbusEndianess.LittleEndian)
                throw new InvalidDataException("Wrong endianess");
            if (fixedLengthHeader.ProtocolVersion != DbusProtocolVersion.Default)
                throw new InvalidDataException("Wrong protocol version");

            // Store values before receiving the next header
            var messageType = fixedLengthHeader.MessageType;
            var serial = fixedLengthHeader.Serial;
            var shouldSendReply = !fixedLengthHeader.Flags.HasFlag(DbusMessageFlags.NoReplyExpected);
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
                case DbusMessageType.MethodCall:
                    var methodCallOptions = new MethodCallOptions(header, shouldSendReply, serial);
                    var receivedMessage = new ReceivedMessage(header, decoder);
                    handleMethodCall(
                        methodCallOptions,
                        receivedMessage
                    );
                    break;
                case DbusMessageType.MethodReturn:
                    handleMethodReturn(header, decoder);
                    break;
                case DbusMessageType.Error:
                    handleError(header, decoder);
                    break;
                case DbusMessageType.Signal:
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
    }
}
