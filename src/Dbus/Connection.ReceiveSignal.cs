﻿using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        public delegate void SignalHandler(MessageHeader header, Decoder decoder);

        private readonly ConcurrentDictionary<string, SignalHandler> signalHandlers =
            new ConcurrentDictionary<string, SignalHandler>();

        public IDisposable RegisterSignalHandler(
            ObjectPath path,
            string interfaceName,
            string member,
            SignalHandler handler
        )
        {
            var dictionaryEntry = path + "\0" + interfaceName + "\0" + member;
            signalHandlers.AddOrUpdate(
                dictionaryEntry,
                handler,
                (_, existingHandler) => existingHandler + handler
            );

            var match = $"type='signal',interface='{interfaceName}',member={member},path='{path}'";
            var canRegister = orgFreedesktopDbus != null;
            if (canRegister)
                Task.Run(() => orgFreedesktopDbus.AddMatchAsync(match));

            return deregisterVia(deregister);

            void deregister()
            {
                if (canRegister)
                    Task.Run(() => orgFreedesktopDbus.RemoveMatchAsync(match));
                SignalHandler current;
                do
                {
                    signalHandlers.TryGetValue(dictionaryEntry, out current);
                } while (!signalHandlers.TryUpdate(dictionaryEntry, current - handler, current));
            }
        }

        private void handleSignal(
            MessageHeader header,
            IMemoryOwner<byte> body,
            int bodyLength
        )
        {
            var dictionaryEntry = header.Path + "\0" + header.InterfaceName + "\0" + header.Member;
            if (signalHandlers.TryGetValue(dictionaryEntry, out var handler))
                Task.Run(() =>
                {
                    handler(header, new Decoder(body.Memory, bodyLength));
                    body.Dispose();
                });
        }
    }
}
