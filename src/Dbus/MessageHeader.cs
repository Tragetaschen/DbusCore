using DotNetCross.NativeInts;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Dbus
{
    public class MessageHeader
    {
        private static readonly int sizeofCmsghdr = Unsafe.SizeOf<cmsghdr>();

        private readonly SocketOperations socketOperations;

        public MessageHeader(
            SocketOperations socketOperations,
            ReadOnlySpan<byte> headerBytes,
            ReadOnlySpan<byte> controlBytes
        )
        {
            this.socketOperations = socketOperations;
            BodySignature = "";
            var index = 0;
            while (index < headerBytes.Length)
            {
                var typeCode = Decoder.GetByte(headerBytes, ref index);
                var signature = Decoder.GetSignature(headerBytes, ref index);
                switch (typeCode)
                {
                    case 1:
                        Path = Decoder.GetObjectPath(headerBytes, ref index);
                        break;
                    case 2:
                        InterfaceName = Decoder.GetString(headerBytes, ref index);
                        break;
                    case 3:
                        Member = Decoder.GetString(headerBytes, ref index);
                        break;
                    case 4:
                        ErrorName = Decoder.GetString(headerBytes, ref index);
                        break;
                    case 5:
                        ReplySerial = Decoder.GetUInt32(headerBytes, ref index);
                        break;
                    case 6:
                        Destination = Decoder.GetString(headerBytes, ref index);
                        break;
                    case 7:
                        Sender = Decoder.GetString(headerBytes, ref index);
                        break;
                    case 8:
                        BodySignature = Decoder.GetSignature(headerBytes, ref index);
                        break;
                    case 9:
                        var numberOfFds = Decoder.GetUInt32(headerBytes, ref index);

                        var cmsgHeaderBytes = controlBytes.Slice(0, sizeofCmsghdr);
                        var cmsgHeader = MemoryMarshal.Cast<byte, cmsghdr>(cmsgHeaderBytes);

                        var fileDescriptorsBytes = controlBytes.Slice(
                            sizeofCmsghdr,
                            cmsgHeader.Length - sizeofCmsghdr
                        );
                        var fileDescriptors = MemoryMarshal.Cast<byte, int>(fileDescriptorsBytes);

                        System.Diagnostics.Debug.Assert(numberOfFds == fileDescriptors.Length);

                        UnixFds = new SafeHandle[numberOfFds];
                        for (var i = 0; i < numberOfFds; ++i)
                            UnixFds[i] = new ReceivedFileDescriptorSafeHandle(fileDescriptors[i]);
                        break;
                }
                Alignment.Advance(ref index, 8);
            }
        }
        public ObjectPath Path { get; }
        public string InterfaceName { get; }
        public string Member { get; }
        public string ErrorName { get; }
        public uint ReplySerial { get; }
        public string Destination { get; }
        public string Sender { get; }
        public Signature BodySignature { get; }
        public SafeHandle[] UnixFds { get; }

        public override string ToString()
            => $"P: {Path}, I: {InterfaceName}, M: {Member}, E: {ErrorName}, R: {ReplySerial}, D: {Destination}, S: {Sender}, B: {BodySignature}";

        public Stream GetStreamFromFd(int index) =>
            new UnixFdStream(UnixFds[index], socketOperations);

        private struct cmsghdr
        {
#pragma warning disable 0649
            public nint len; // size_t, not! socklen_t
            public int level;
            public int type;
#pragma warning restore
        }
    }
}
