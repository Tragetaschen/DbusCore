using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public partial class Connection
    {
        public delegate void SignalHandler(ReceivedMessage message);

        private readonly ConcurrentDictionary<string, SignalHandler?> signalHandlers =
            new ConcurrentDictionary<string, SignalHandler?>();

        public IDisposable RegisterSignalHandler(
            ObjectPath path,
            string interfaceName,
            string member,
            SignalHandler handler
        )
        {
            var dictionaryEntry = path + "\0" + interfaceName + "\0" + member;
            var newHandler = signalHandlers.AddOrUpdate(
                dictionaryEntry,
                handler,
                (_, existingHandler) => existingHandler + handler
            );

            var match = $"type='signal',interface='{interfaceName}',member={member},path='{path}'";
            var addMatchTask = Task.CompletedTask;
            if (newHandler == handler)
            {
                // AddOrUpdate added a new key
                addMatchTask = orgFreedesktopDbus.AddMatchAsync(match, default);
            }

            return deregisterVia(deregister);

            void deregister()
            {
                SignalHandler? current;
                SignalHandler? updated;
                do
                {
                    signalHandlers.TryGetValue(dictionaryEntry, out current);
                    updated = current - handler;
                } while (!signalHandlers.TryUpdate(dictionaryEntry, updated, current));
                if (updated == null)
                    // TryUpdate set the key's value to null thus removing the last entry
                    Task.Run(async () =>
                    {
                        await addMatchTask;
                        await orgFreedesktopDbus.RemoveMatchAsync(match, default);
                    });
            }
        }

        private void handleSignal(
            MessageHeader header,
            Decoder decoder,
            CancellationToken cancellationToken
        )
        {
            var dictionaryEntry = header.Path + "\0" + header.InterfaceName + "\0" + header.Member;
            if (signalHandlers.TryGetValue(dictionaryEntry, out var handlers) && handlers != null)
                Task.Run(() =>
                {
                    var message = new ReceivedMessage(header, decoder);
                    using (message)
                        foreach (SignalHandler handler in handlers.GetInvocationList())
                            try
                            {
                                handler(message);
                                decoder.Reset();
                            }
                            catch (Exception e)
                            {
                                onUnobservedException(e);
                            }
                }, cancellationToken);
            else
                decoder.Dispose();
        }
    }
}
