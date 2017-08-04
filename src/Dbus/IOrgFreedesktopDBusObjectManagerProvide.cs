using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IOrgFreedesktopDbusObjectManagerProvide : IDisposable
    {
        ObjectPath Root { get; }
        Task<IDictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync();
        string AddObject<TInterface, TImplementation>(TImplementation instance, ObjectPath path) where TImplementation : TInterface;
    }
}
