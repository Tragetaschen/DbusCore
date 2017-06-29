using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public interface IOrgFreedesktopDbusObjectManagerProvide
    {
        ObjectPath Root { get; }
        Task<Dictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync();
    }
}
