﻿using DotNetCross.NativeInts;
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
                var value = Decoder.GetObject(headerBytes, ref index);
                switch (typeCode)
                {
                    case 1:
                        Path = (ObjectPath)value;
                        break;
                    case 2:
                        InterfaceName = (string)value;
                        break;
                    case 3:
                        Member = (string)value;
                        break;
                    case 4:
                        ErrorName = (string)value;
                        break;
                    case 5:
                        ReplySerial = (uint)value;
                        break;
                    case 6:
                        Destination = (string)value;
                        break;
                    case 7:
                        Sender = (string)value;
                        break;
                    case 8:
                        BodySignature = (Signature)value;
                        break;
                    case 9:
                        var numberOfFds = (uint)value;

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
