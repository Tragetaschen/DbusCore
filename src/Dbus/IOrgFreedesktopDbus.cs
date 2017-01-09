using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    [DbusConsume("org.freedesktop.DBus", Path = "/org/freedesktop/DBus", Destination = "org.freedesktop.DBus")]
    public interface IOrgFreedesktopDbus : IDisposable
    {
        event Action<string> NameAcquired;

        Task<string> HelloAsync();
        Task<IEnumerable<string>> ListNamesAsync();
        Task<uint> RequestNameAsync(string name, uint flags);
    }
}