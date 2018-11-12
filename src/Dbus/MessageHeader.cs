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
                var typeCode = (DbusHeaderType)header.GetByte();
                var signature = header.GetSignature();
                switch (typeCode)
                {
                    case DbusHeaderType.Path:
                        Path = header.GetObjectPath();
                        break;
                    case DbusHeaderType.InterfaceName:
                        InterfaceName = header.GetString();
                        break;
                    case DbusHeaderType.Member:
                        Member = header.GetString();
                        break;
                    case DbusHeaderType.ErrorName:
                        ErrorName = header.GetString();
                        break;
                    case DbusHeaderType.ReplySerial:
                        ReplySerial = header.GetUInt32();
                        break;
                    case DbusHeaderType.Destination:
                        Destination = header.GetString();
                        break;
                    case DbusHeaderType.Sender:
                        Sender = header.GetString();
                        break;
                    case DbusHeaderType.Signature:
                        BodySignature = header.GetSignature();
                        break;
                    case DbusHeaderType.UnixFds:
                        var numberOfFds = header.GetUInt32();

                        var cmsgHeaderBytes = controlBytes.Slice(0, sizeofCmsghdr);
                        var cmsgHeader = MemoryMarshal.Cast<byte, cmsghdr>(cmsgHeaderBytes);

                        var fileDescriptorsBytes = controlBytes.Slice(
                            sizeofCmsghdr,
                            controlBytes.Length - sizeofCmsghdr
                        );
                        var fileDescriptors = MemoryMarshal.Cast<byte, int>(fileDescriptorsBytes);

                        System.Diagnostics.Debug.Assert(numberOfFds == fileDescriptors.Length);

                        UnixFds = new SafeHandle[numberOfFds];
                        for (var i = 0; i < numberOfFds; ++i)
                            UnixFds[i] = new ReceivedFileDescriptorSafeHandle(fileDescriptors[i]);
                        break;
                }
                header.AdvanceToCompoundValue();
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
