﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    [DbusConsume("org.freedesktop.DBus.ObjectManager")]
    public interface IOrgFreedesktopDbusObjectManager : IDisposable
    {
        [DbusMethod]
        Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync(CancellationToken cancellationToken);
        event Action<ObjectPath, IDictionary<string, IDictionary<string, object>>> InterfacesAdded;
        event Action<ObjectPath, IEnumerable<string>> InterfacesRemoved;
    }
}
