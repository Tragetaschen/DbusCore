using DotNetCross.NativeInts;
using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Dbus
{
    public class MessageHeader
    {
        private static readonly int sizeofCmsghdr = Marshal.SizeOf<cmsghdr>();

        public MessageHeader(
            SocketOperations socketOperations,
            Decoder header,
            ReadOnlySpan<byte> controlBytes
        )
        {
            SocketOperations = socketOperations;
            BodySignature = "";
            while (!header.IsFinished)
            {
                var typeCode = (DbusHeaderType)Decoder.GetByte(header);
                var signature = Decoder.GetSignature(header); // variant signature
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

                        var cmsgHeaderBytes = controlBytes[..sizeofCmsghdr];
                        var cmsgHeader = MemoryMarshal.Cast<byte, cmsghdr>(cmsgHeaderBytes);

                        var fileDescriptorsBytes = controlBytes[sizeofCmsghdr..(int)cmsgHeader[0].len];
                        var fileDescriptors = MemoryMarshal.Cast<byte, int>(fileDescriptorsBytes);
                        System.Diagnostics.Debug.Assert(numberOfFds == fileDescriptors.Length);

                        UnixFds = new SafeHandle[numberOfFds];
                        for (var i = 0; i < numberOfFds; ++i)
                            UnixFds[i] = new SafeFileHandle(new IntPtr(fileDescriptors[i]), true);
                        break;
                    default:
                        var value = Decoder.DecodeVariant(header, signature);
                        Console.Error.WriteLine("Unknown header type {0} with value '{1}'. Please update the implementation", typeCode, value);
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
        internal SocketOperations SocketOperations { get; }

        public override string ToString()
            => $"P: {Path}, I: {InterfaceName}, M: {Member}, E: {ErrorName}, R: {ReplySerial}, D: {Destination}, S: {Sender}, B: {BodySignature}";

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
