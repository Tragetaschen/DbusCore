using DotNetCross.NativeInts;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Dbus
{
    public class MessageHeader
    {
        private static readonly int sizeofCmsghdr = Marshal.SizeOf<cmsghdr>();

        private readonly SocketOperations socketOperations;

        public MessageHeader(
            SocketOperations socketOperations,
            Decoder header,
            ReadOnlySpan<byte> controlBytes
        )
        {
            this.socketOperations = socketOperations;
            BodySignature = "";
            while (!header.IsFinished)
            {
                var typeCode = header.GetByte();
                var signature = header.GetSignature();
                switch (typeCode)
                {
                    case 1:
                        Path = header.GetObjectPath();
                        break;
                    case 2:
                        InterfaceName = header.GetString();
                        break;
                    case 3:
                        Member = header.GetString();
                        break;
                    case 4:
                        ErrorName = header.GetString();
                        break;
                    case 5:
                        ReplySerial = header.GetUInt32();
                        break;
                    case 6:
                        Destination = header.GetString();
                        break;
                    case 7:
                        Sender = header.GetString();
                        break;
                    case 8:
                        BodySignature = header.GetSignature();
                        break;
                    case 9:
                        var numberOfFds = header.GetUInt32();

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
                header.AdvanceToAlignment(8);
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

        public Stream GetStream(int index) =>
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
