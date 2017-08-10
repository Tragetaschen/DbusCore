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
            const int iovecsLength = 3;

            var fixedLengthHeader = new byte[fixedLengthHeaderLength]; // header up until the array length
            var control = stackalloc int[controlLength];
            var iovecs = stackalloc UnsafeNativeMethods.iovec[iovecsLength];

            var hasValidFixedHeader = false;

            while (true)
            {
                if (!hasValidFixedHeader)
                    fixed (byte* fixedLengthHeaderP = fixedLengthHeader)
                    {
                        iovecs[0].iov_base = fixedLengthHeaderP;
                        iovecs[0].iov_len = fixedLengthHeaderLength;

                        var msg = new UnsafeNativeMethods.msghdr
                        {
                            iov = iovecs,
                            iovlen = 1,
                            controllen = controlLength * sizeof(int),
                            control = control
                        };
                        var length = UnsafeNativeMethods.recvmsg(socketHandle, ref msg, 0);
                        if (length == 0) // socket shutdown
                            return;
                        if (length < 0) // read error
                            throw new InvalidOperationException("recvmsg failed with " + length);
                    }

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
                {
                    iovecs[0].iov_base = headerP;
                    iovecs[0].iov_len = receivedArrayLength;
                    iovecs[1].iov_base = bodyP;
                    iovecs[1].iov_len = bodyLength;
                    iovecs[2].iov_base = fixedLengthHeaderP;
                    iovecs[2].iov_len = fixedLengthHeaderLength;
                    var nextMsg = new UnsafeNativeMethods.msghdr
                    {
                        iov = iovecs,
                        iovlen = iovecsLength,
                        control = control,
                        controllen = controlLength * sizeof(int),
                    };
                    var length = UnsafeNativeMethods.recvmsg(socketHandle, ref nextMsg, 0);
                    if (length == 0) // socket shutdown
                        return;
                    if (length < 0) // read error
                        throw new InvalidOperationException("recvmsg failed with " + length);
                    hasValidFixedHeader = length == receivedArrayLength + bodyLength + fixedLengthHeader.Length;
                }

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
