using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dbus
{
    public partial class Connection
    {
        [DllImport("libc")]
        private static extern int recvmsg(IntPtr sockfd, [In] ref msghdr buf, int flags);

        private unsafe struct iovec
        {
            public byte* iov_base;
            public int iov_len;
        }

        private unsafe struct msghdr
        {
            public IntPtr name;
            public int namelen;
            public iovec* iov;
            public int iovlen;
            public int[] control;
            public int controllen;
            public int flags;
        }

        private unsafe void receive()
        {
            var fixedLengthHeader = new byte[16]; // header up until the array length
            var token = receiveCts.Token;
            var control = new int[16];

            var hasValidFixedHeader = false;

            while (!token.IsCancellationRequested)
            {
                if (!hasValidFixedHeader)
                    fixed (byte* fixedLengthHeaderP = fixedLengthHeader)
                    {
                        var iovecs = stackalloc iovec[1];
                        iovecs[0].iov_base = fixedLengthHeaderP;
                        iovecs[0].iov_len = 16;

                        var msg = new msghdr
                        {
                            iov = iovecs,
                            iovlen = 1,
                            controllen = control.Length * sizeof(int),
                            control = control
                        };
                        if (recvmsg(socketHandle, ref msg, 0) <= 0)
                            return;
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
                    var iovecs = stackalloc iovec[3];
                    iovecs[0].iov_base = headerP;
                    iovecs[0].iov_len = receivedArrayLength;
                    iovecs[1].iov_base = bodyP;
                    iovecs[1].iov_len = bodyLength;
                    iovecs[2].iov_base = fixedLengthHeaderP;
                    iovecs[2].iov_len = 16;
                    var nextMsg = new msghdr
                    {
                        iov = iovecs,
                        iovlen = 3,
                        control = control,
                        controllen = control.Length * sizeof(int),
                    };
                    var len = recvmsg(socketHandle, ref nextMsg, 0);
                    if (len <= 0)
                        return;
                    hasValidFixedHeader = len == receivedArrayLength + bodyLength + fixedLengthHeader.Length;
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
