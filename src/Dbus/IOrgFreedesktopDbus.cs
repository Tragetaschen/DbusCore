using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    [DbusConsume("org.freedesktop.DBus", Path = "/org/freedesktop/DBus", Destination = "org.freedesktop.DBus")]
    public interface IOrgFreedesktopDbus : IDisposable
    {
        event Action<string> NameAcquired;

        Task AddMatchAsync(string match, CancellationToken cancellationToken = default);
        Task RemoveMatchAsync(string match, CancellationToken cancellationToken = default);
        Task<string> HelloAsync(CancellationToken cancellationToken);
        Task<IEnumerable<string>> ListNamesAsync(CancellationToken cancellationToken);
        Task<uint> RequestNameAsync(string name, uint flags, CancellationToken cancellationToken);
    }
}
