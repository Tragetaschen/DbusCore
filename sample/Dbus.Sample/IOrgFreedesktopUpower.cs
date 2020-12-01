using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus.Sample
{
    [DbusConsume("org.freedesktop.UPower", Path = "/org/freedesktop/UPower", Destination = "org.freedesktop.UPower")]
    public interface IOrgFreedesktopUpower: IDisposable, IAsyncDisposable
    {
        Task<Dictionary<string, object>> GetAllAsync(string interfaceName);
    }
}
