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

        public IAsyncDisposable RegisterSignalHandler(
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

            return new signalHandle(
                this,
                dictionaryEntry,
                match,
                addMatchTask,
                handler
            );
        }

        private class signalHandle : IAsyncDisposable
        {
            private readonly Connection connection;
            private readonly string entry;
            private readonly string match;
            private readonly Task addMatchTask;
            private readonly SignalHandler handler;

            public signalHandle(
                Connection connection,
                string entry,
                string match,
                Task addMatchTask,
                SignalHandler handler
            )
            {
                this.connection = connection;
                this.entry = entry;
                this.match = match;
                this.addMatchTask = addMatchTask;
                this.handler = handler;
            }

            public async ValueTask DisposeAsync()
            {
                SignalHandler? current;
                SignalHandler? updated;
                do
                {
                    connection.signalHandlers.TryGetValue(entry, out current);
                    updated = current - handler;
                } while (!connection.signalHandlers.TryUpdate(entry, updated, current));
                if (updated == null)
                {
                    await addMatchTask;
                    // TryUpdate set the key's value to null thus removing the last entry
                    await connection.orgFreedesktopDbus.RemoveMatchAsync(match, default);
                }
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
