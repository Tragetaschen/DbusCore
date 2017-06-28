using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public class OrgFreedesktopDbusObjectManager : IOrgFreedesktopDbusObjectManagerProvide
    {

        public ObjectPath root { get; }

        private readonly Connection connection;
        private readonly Dictionary<ObjectPath, List<IProxy>> managedObjects;
        //        private Dictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>> managedObjects;

        public OrgFreedesktopDbusObjectManager(Connection connection, ObjectPath root)
        {
            this.connection = connection;
            this.root = root;
            managedObjects = new Dictionary<ObjectPath, List<IProxy>>() { };
        }

        public event Action<ObjectPath, IDictionary<string, IDictionary<string, object>>> InterfacesAdded;
        public event Action<ObjectPath, IEnumerable<string>> InterfacesRemoved;

        public void AddObject<TInterface, TImplementation>(TImplementation instance, ObjectPath path) where TImplementation : TInterface
        {
            string editedPath;
            if (path.ToString() == "/")
                editedPath = "";
            else
                editedPath = path.ToString();
            var proxy = (IProxy)connection.Publish<TInterface>(instance, root.ToString() + editedPath);
            if (managedObjects.ContainsKey(root.ToString() + editedPath))
            {
                managedObjects[root.ToString() + editedPath].Add((proxy));
            }
            else
            {
                managedObjects.Add(root.ToString() + editedPath, new List<IProxy>() { (proxy) });
            }
        }

        public async Task<Dictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync()
        {
            await Task.Delay(1000);
            return managedObjects;
        }

        public void Dispose() =>
            throw new NotImplementedException();
    }
}
