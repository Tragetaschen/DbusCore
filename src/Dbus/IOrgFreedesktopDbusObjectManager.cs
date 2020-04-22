using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    [DbusConsume("org.freedesktop.DBus.ObjectManager")]
    public interface IOrgFreedesktopDbusObjectManager : IAsyncDisposable, IDisposable
    {
        [DbusMethod]
        Task<Dictionary<ObjectPath, Dictionary<string, Dictionary<string, object>>>> GetManagedObjectsAsync(CancellationToken cancellationToken);
        event Action<ObjectPath, Dictionary<string, Dictionary<string, object>>> InterfacesAdded;
        event Action<ObjectPath, List<string>> InterfacesRemoved;
    }
}
