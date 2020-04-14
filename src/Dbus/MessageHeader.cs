using DotNetCross.NativeInts;
using Microsoft.Win32.SafeHandles;
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
            ReadOnlySpan<byte> controlBytes,
            bool isMonoRuntime
        )
        {
            this.socketOperations = socketOperations;
            BodySignature = "";
            while (!header.IsFinished)
            {
                var typeCode = (DbusHeaderType)Decoder.GetByte(header);
                Decoder.GetSignature(header); // variant signature
                switch (typeCode)
                {
                    case DbusHeaderType.Path:
                        Path = Decoder.GetObjectPath(header);
                        break;
                    case DbusHeaderType.InterfaceName:
                        InterfaceName = Decoder.GetString(header);
                        break;
                    case DbusHeaderType.Member:
                        Member = Decoder.GetString(header);
                        break;
                    case DbusHeaderType.ErrorName:
                        ErrorName = Decoder.GetString(header);
                        break;
                    case DbusHeaderType.ReplySerial:
                        ReplySerial = Decoder.GetUInt32(header);
                        break;
                    case DbusHeaderType.Destination:
                        Destination = Decoder.GetString(header);
                        break;
                    case DbusHeaderType.Sender:
                        Sender = Decoder.GetString(header);
                        break;
                    case DbusHeaderType.Signature:
                        BodySignature = Decoder.GetSignature(header);
                        break;
                    case DbusHeaderType.UnixFds:
                        var numberOfFds = Decoder.GetUInt32(header);

                        var cmsgHeaderBytes = controlBytes.Slice(0, sizeofCmsghdr);
                        var cmsgHeader = MemoryMarshal.Cast<byte, cmsghdr>(cmsgHeaderBytes);

                        var fileDescriptorsBytes = controlBytes.Slice(
                            sizeofCmsghdr,
                            (int)cmsgHeader[0].len - sizeofCmsghdr
                        );
                        var fileDescriptors = MemoryMarshal.Cast<byte, int>(fileDescriptorsBytes);
                        System.Diagnostics.Debug.Assert(numberOfFds == fileDescriptors.Length);

                        UnixFds = new SafeHandle[numberOfFds];
                        for (var i = 0; i < numberOfFds; ++i)
                            if (isMonoRuntime)
                                UnixFds[i] = new ReceivedFileDescriptorSafeHandle(fileDescriptors[i]);
                            else
                                UnixFds[i] = new SafeFileHandle(new IntPtr(fileDescriptors[i]), true);
                        break;
                }
                Decoder.AdvanceToCompoundValue(header);
            }
        }
        public ObjectPath? Path { get; }
        public string? InterfaceName { get; }
        public string? Member { get; }
        public string? ErrorName { get; }
        public uint ReplySerial { get; }
        public string? Destination { get; }
        public string? Sender { get; }
        public Signature? BodySignature { get; }
        public SafeHandle[]? UnixFds { get; }

        public override string ToString()
            => $"P: {Path}, I: {InterfaceName}, M: {Member}, E: {ErrorName}, R: {ReplySerial}, D: {Destination}, S: {Sender}, B: {BodySignature}";

        public Stream GetStream(int index)
        {
            if (UnixFds == null)
                throw new InvalidOperationException("Now file descriptors received");
            return new UnixFdStream(UnixFds[index], socketOperations);
        }

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
