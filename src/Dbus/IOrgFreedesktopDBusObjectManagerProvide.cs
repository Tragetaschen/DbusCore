using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus;

public interface IOrgFreedesktopDbusObjectManagerProvide : IDisposable
{
    ObjectPath Root { get; }
    Task<Dictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync(CancellationToken cancellationToken);
    ObjectPath AddObject<TInterface>(TInterface instance, ObjectPath? path = null) where TInterface : class;
    void RemoveObject<TInterface>(TInterface instance, ObjectPath? path = null) where TInterface : class;
}
