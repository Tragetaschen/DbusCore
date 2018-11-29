using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IOrgFreedesktopDbusObjectManagerProvide : IDisposable
    {
        ObjectPath Root { get; }
        Task<IDictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync(CancellationToken cancellationToken);
        ObjectPath AddObject<TInterface>(TInterface instance, ObjectPath path);
        void RemoveObject<TInterface>(TInterface instance, ObjectPath path);
    }
}
