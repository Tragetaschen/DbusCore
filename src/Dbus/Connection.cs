using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        private Socket socket;
        private Stream stream;
        private static readonly Encoding encoding = Encoding.UTF8;

        private Connection()
        {
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
            socket.Connect(new systemBusEndPoint());
            stream = new LoggingStream(new NetworkStream(socket));
        }

        public async static Task<Connection> CreateAsync()
        {
            var result = new Connection();
            await authenticate(result.stream).ConfigureAwait(false);
            return result;
        }

        private int calculateRequiredAlignment(int position, int alignment)
        {
            var bytesIntoAlignment = position & alignment - 1;
            if (bytesIntoAlignment == 0)
                return 0;
            else
                return alignment - bytesIntoAlignment;
        }

        private int ensureAlignment(List<byte> message, int alignment)
        {
            var result = calculateRequiredAlignment(message.Count, alignment);
            for (var i = 0; i < result; ++i)
                message.Add(0);
            return result;
        }

        private int addStringVariant(List<byte> message, byte signature, string s)
        {
            message.Add(1); // signature length
            message.Add(signature);
            message.Add(0);
            var result = 3;
            result += ensureAlignment(message, 4);
            var bytes = encoding.GetBytes(s);
            message.AddRange(BitConverter.GetBytes(bytes.Length));
            result += 4;
            message.AddRange(bytes);
            result += bytes.Length;
            message.Add(0);
            ++result;
            return result;
        }

        public async Task<string> HelloAsync()
        {
            var serial = 42;

            var header = new List<byte>();
            header.Add((byte)'l'); // little endian
            header.Add(1); // method call
            header.Add(0); // flags
            header.Add(1); // protocol version
            header.AddRange(BitConverter.GetBytes(0)); // body length
            header.AddRange(BitConverter.GetBytes(serial)); // serial

            var arrayLength = 0;
            header.AddRange(BitConverter.GetBytes(0)); // array length
            arrayLength += ensureAlignment(header, 8);
            header.Add(1); // path
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'o', "/org/freedesktop/DBus");
            arrayLength += ensureAlignment(header, 8);
            header.Add(2); // interface
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', "org.freedesktop.DBus");
            arrayLength += ensureAlignment(header, 8);
            header.Add(3); // member
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', "Hello");
            arrayLength += ensureAlignment(header, 8);
            header.Add(6); // destination
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', "org.freedesktop.DBus");
            ensureAlignment(header, 8); // final padding

            var realLength = BitConverter.GetBytes(arrayLength);
            header[12] = realLength[0];
            header[13] = realLength[1];
            header[14] = realLength[2];
            header[15] = realLength[3];

            var buffer = header.ToArray();
            await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);


            var fixedLengthHeader = new byte[16]; // header up until the array length
            await stream.ReadAsync(fixedLengthHeader, 0, fixedLengthHeader.Length).ConfigureAwait(false);

            if (fixedLengthHeader[0] != (byte)'l')
                throw new InvalidDataException("Wrong endianess");
            if (fixedLengthHeader[1] != 2)
                throw new InvalidDataException("Not a method return");
            if (fixedLengthHeader[3] != 1)
                throw new InvalidDataException("Wrong protocol version");
            var bodyLength = BitConverter.ToInt32(fixedLengthHeader, 4);
            //var receivedSerial = BitConverter.ToInt32(fixedLengthHeader, 8);
            var receivedArrayLength = BitConverter.ToInt32(fixedLengthHeader, 12);

            var arrayData = new byte[receivedArrayLength + calculateRequiredAlignment(receivedArrayLength, 8)];
            await stream.ReadAsync(arrayData, 0, arrayData.Length).ConfigureAwait(false);

            var body = new byte[bodyLength];
            await stream.ReadAsync(body, 0, body.Length).ConfigureAwait(false);
            var stringLength = BitConverter.ToInt32(body, 0);
            var path = encoding.GetString(body, 4, stringLength);

            return path;
        }

        public void Dispose()
        {
            stream.Dispose();
            socket.Dispose();
        }

        private class systemBusEndPoint : EndPoint
        {
            public override SocketAddress Serialize()
            {
                var socketFile = encoding.GetBytes("/var/run/dbus/system_bus_socket");
                var result = new SocketAddress(AddressFamily.Unix, socketFile.Length + 2);
                for (var i = 0; i < socketFile.Length; ++i)
                    result[i + 2] = socketFile[i];
                return result;
            }
        }
    }
}
