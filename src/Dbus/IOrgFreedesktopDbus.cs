using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IOrgFreedesktopDbus : IDisposable, IAsyncDisposable
    {
        Task AddMatchAsync(string match, CancellationToken cancellationToken);
        Task RemoveMatchAsync(string match, CancellationToken cancellationToken);
        Task<List<string>> ListNamesAsync(CancellationToken cancellationToken);
    }
}
