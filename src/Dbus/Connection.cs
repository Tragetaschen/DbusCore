﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection : IDisposable
    {
        private Socket socket;
        private Stream stream;
        private int serialCounter;
        private static readonly Encoding encoding = Encoding.UTF8;
        private ConcurrentDictionary<int, TaskCompletionSource<ReceivedMethodReturn>> expectedMessages;

        private Connection()
        {
            expectedMessages = new ConcurrentDictionary<int, TaskCompletionSource<ReceivedMethodReturn>>();
            socket = new Socket(AddressFamily.Unix, SocketType.Stream, 0);
            socket.Connect(new systemBusEndPoint());
            stream = new LoggingStream(new NetworkStream(socket));

            Task.Run(receive);
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

        public async Task<ReceivedMethodReturn> SendMethodCall(
            string path,
            string interfaceName,
            string methodName,
            string destination
        )
        {
            var serial = Interlocked.Increment(ref serialCounter);

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
            arrayLength += addStringVariant(header, (byte)'o', path);
            arrayLength += ensureAlignment(header, 8);
            header.Add(2); // interface
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', interfaceName);
            arrayLength += ensureAlignment(header, 8);
            header.Add(3); // member
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', methodName);
            arrayLength += ensureAlignment(header, 8);
            header.Add(6); // destination
            ++arrayLength;
            arrayLength += addStringVariant(header, (byte)'s', destination);
            ensureAlignment(header, 8); // final padding

            var realLength = BitConverter.GetBytes(arrayLength);
            header[12] = realLength[0];
            header[13] = realLength[1];
            header[14] = realLength[2];
            header[15] = realLength[3];

            var tcs = new TaskCompletionSource<ReceivedMethodReturn>();
            expectedMessages[serial] = tcs;

            var buffer = header.ToArray();
            await stream.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

            return await tcs.Task.ConfigureAwait(false);
        }

        private async Task receive()
        {
            var fixedLengthHeader = new byte[16]; // header up until the array length
            while (true)
            {
                Console.WriteLine("start reading");
                await stream.ReadAsync(fixedLengthHeader, 0, fixedLengthHeader.Length).ConfigureAwait(false);
                Console.WriteLine("end reading");

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

                var serial = 0;
                var bodySignature = string.Empty;
                var index = 0;
                while (index < receivedArrayLength)
                {
                    var headerFieldTypeCode = arrayData[index];
                    index += 4;
                    switch (headerFieldTypeCode)
                    {
                        case 1: /* PATH: OBJECT_PATH */
                        case 2: /* INTERFACE: STRING */
                        case 3: /* MEMBER: STRING */
                        case 4: /* ERROR_NAME: STRING */
                        case 6: /* DESTINATION: STRING */
                        case 7: /* SENDER: STRING */
                            var stringLength = BitConverter.ToInt32(arrayData, index);
                            index += 4 /* length */ + stringLength + 1 /* null byte*/;
                            break;
                        case 8: /* SIGNATURE: SIGNATURE */
                            var signatureLength = arrayData[index];
                            bodySignature = encoding.GetString(arrayData, index + 1, signatureLength);
                            index += signatureLength + 1 /* null byte */;
                            break;
                        case 5: /* REPLY_SERIAL: UINT32 */
                            serial = BitConverter.ToInt32(arrayData, index);
                            index += 4;
                            break;
                        case 9: /* UNIX_FDS: UINT32 */
                            index += 4;
                            break;
                    }
                    index += calculateRequiredAlignment(index, 8);
                }

                TaskCompletionSource<ReceivedMethodReturn> tcs;
                if (!expectedMessages.TryRemove(serial, out tcs))
                    throw new InvalidOperationException("Couldn't find the method call for the method return");
                var receivedMessage = new ReceivedMethodReturn
                {
                    Body = body,
                    Signature = bodySignature,
                };
                Console.WriteLine("set result");
                try
                {
                    tcs.SetResult(receivedMessage);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                Console.WriteLine("next round");
            }
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
