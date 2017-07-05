﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        private readonly ConcurrentDictionary<string, IProxy> objectProxies =
            new ConcurrentDictionary<string, IProxy>();

        public IDisposable RegisterObjectProxy(
            ObjectPath path,
            string interfaceName,
            IProxy proxy
        )
        {
            var dictionaryEntry = path + "\0" + interfaceName;
            if (!objectProxies.TryAdd(dictionaryEntry, proxy))
                throw new InvalidOperationException("Attempted to register an object proxy twice");

            return new deregistration
            {
                Deregister = () =>
                {
                    objectProxies.TryRemove(dictionaryEntry, out var _);
                }
            };
        }

        public Task SendMethodReturnAsync(uint replySerial, string destination, List<byte> body, Signature signature)
        {
            var serial = getSerial();

            var message = Encoder.StartNew();
            var index = 0;
            Encoder.Add(message, ref index, (byte)'l'); // little endian
            Encoder.Add(message, ref index, (byte)2); // method return
            Encoder.Add(message, ref index, (byte)1); // no reply expected
            Encoder.Add(message, ref index, (byte)1); // protocol version
            Encoder.Add(message, ref index, body.Count); // Actually uint
            Encoder.Add(message, ref index, serial); // Actually uint

            Encoder.AddArray(message, ref index, (List<byte> buffer, ref int localIndex) =>
            {
                addHeader(buffer, ref localIndex, 6, destination);
                addHeader(buffer, ref localIndex, replySerial);
                if (body.Count > 0)
                    addHeader(buffer, ref localIndex, signature);
            });
            Encoder.EnsureAlignment(message, ref index, 8);
            message.AddRange(body);

            var messageArray = message.ToArray();
            return serializedWriteToStream(messageArray);
        }

        private void handleMethodCall(uint replySerial, MessageHeader header, byte[] body, bool shouldSendReply)
        {
            if (header.InterfaceName == "org.freedesktop.DBus.Properties")
            {
                Task.Run(() =>
                    handlePropertyRequestAsync(replySerial, header, body));
                return;
            }
            var dictionaryEntry = header.Path + "\0" + header.InterfaceName;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
                Task.Run(() =>
                {
                    try
                    {
                        return proxy.HandleMethodCallAsync(replySerial, header, body, shouldSendReply);
                    }
                    catch (DbusException dbusException)
                    {
                        return sendMethodCallErrorAsync(
                            replySerial,
                            header.Sender,
                            dbusException.ErrorName,
                            dbusException.ErrorMessage
                         );
                    }
                    catch (Exception e)
                    {
                        return sendMethodCallErrorAsync(
                            replySerial,
                            header.Sender,
                            DbusException.CreateErrorName("General"),
                            e.Message
                         );
                    }
                });
            else
                Task.Run(() => sendMethodCallErrorAsync(
                    replySerial,
                    header.Sender,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                ));
        }

        private Task handlePropertyRequestAsync(uint replySerial, MessageHeader header, byte[] body)
        {
            switch (header.Member)
            {
                case "GetAll":
                    return handleGetAllAsync(replySerial, header, body);
                case "Get":
                    return handleGetAsync(replySerial, header, body);
                default:
                    throw new DbusException(
                        DbusException.CreateErrorName("UnknownMethod"),
                        "Method not supported"
                    );
            }
        }
        private Task handleGetAllAsync(uint replySerial, MessageHeader header, byte[] body)
        {
            header.BodySignature.AssertEqual("s");
            var decoderIndex = 0;
            var requestedInterfaces = Decoder.GetString(body, ref decoderIndex);
            var dictionaryEntry = header.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {

                var sendBody = Encoder.StartNew();
                var sendIndex = 0;
                proxy.EncodeProperties(sendBody, ref sendIndex);
                return SendMethodReturnAsync(replySerial, header.Sender, sendBody, "a{sv}");

            }
            else
            {
                return sendMethodCallErrorAsync(
                    replySerial,
                    header.Sender,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                );
            }

        }
        private Task handleGetAsync(uint replySerial, MessageHeader header, byte[] body)
        {
            header.BodySignature.AssertEqual("ss");
            var decoderIndex = 0;
            var requestedInterfaces = Decoder.GetString(body, ref decoderIndex);
            var requestedProperty = Decoder.GetString(body, ref decoderIndex);
            var dictionaryEntry = header.Path + "\0" + requestedInterfaces;
            if (objectProxies.TryGetValue(dictionaryEntry, out var proxy))
            {

                var sendBody = Encoder.StartNew();
                var sendIndex = 0;
                proxy.EncodeProperty(sendBody, ref sendIndex, requestedProperty);
                return SendMethodReturnAsync(replySerial, header.Sender, sendBody, "v");

            }
            else
            {
                return sendMethodCallErrorAsync(
                    replySerial,
                    header.Sender,
                    DbusException.CreateErrorName("MethodCallTargetNotFound"),
                    "The requested method call isn't mapped to an actual object"
                );
            }
        }

        private Task sendMethodCallErrorAsync(uint replySerial, string destination, string error, string errorMessage)
        {
            var serial = getSerial();

            var index = 0;
            var body = Encoder.StartNew();
            Encoder.Add(body, ref index, errorMessage);

            var message = Encoder.StartNew();
            index = 0;
            Encoder.Add(message, ref index, (byte)'l'); // little endian
            Encoder.Add(message, ref index, (byte)3); // error
            Encoder.Add(message, ref index, (byte)1); // no reply expected
            Encoder.Add(message, ref index, (byte)1); // protocol version
            Encoder.Add(message, ref index, body.Count); // Actually uint
            Encoder.Add(message, ref index, serial); // Actually uint

            Encoder.AddArray(message, ref index, (List<byte> buffer, ref int localIndex) =>
            {
                addHeader(buffer, ref localIndex, 6, destination);
                addHeader(buffer, ref localIndex, 4, error);
                addHeader(buffer, ref localIndex, replySerial);
                if (body.Count > 0)
                    addHeader(buffer, ref localIndex, (Signature)"s");
            });
            Encoder.EnsureAlignment(message, ref index, 8);
            message.AddRange(body);

            var messageArray = message.ToArray();
            return serializedWriteToStream(messageArray);
        }
    }
}
