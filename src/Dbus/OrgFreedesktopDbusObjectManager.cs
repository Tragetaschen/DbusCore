using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dbus
{
    public abstract class OrgFreedesktopDbusObjectManager : IOrgFreedesktopDbusObjectManagerProvide
    {
        private readonly Connection connection;
        private readonly Dictionary<ObjectPath, List<IProxy>> managedObjects;

        protected OrgFreedesktopDbusObjectManager(Connection connection, ObjectPath root)
        {
            this.connection = connection;
            Root = root;
            managedObjects = new Dictionary<ObjectPath, List<IProxy>>();
            connection.Publish<IOrgFreedesktopDbusObjectManagerProvide>(this, Root);
        }

        public event Action<ObjectPath, IDictionary<string, IDictionary<string, object>>> InterfacesAdded { add { } remove { } }
        public event Action<ObjectPath, IEnumerable<string>> InterfacesRemoved { add { } remove { } }

        public ObjectPath Root { get; }

        public string AddObject<TInterface, TImplementation>(TImplementation instance, ObjectPath path) where TImplementation : TInterface
        {
            var fullPath = buildFullPath(path);
            var proxy = (IProxy)connection.Publish<TInterface>(instance, fullPath);
            if (managedObjects.ContainsKey(fullPath))
                managedObjects[fullPath].Add(proxy);
            else
                managedObjects.Add(fullPath, new List<IProxy>() { proxy });
            return fullPath;
        }

        private string buildFullPath(ObjectPath path)
        {
            if (path.ToString().StartsWith(Root.ToString()))
                return path.ToString();
            if (!path.ToString().StartsWith("./"))
                throw new ArgumentException("A partial path has to start with ./");
            if (Root == "/")
                return path.ToString().Substring(1);
            if (path.ToString() == "./")
                return Root.ToString();
            return Root + path.ToString().Substring(1);
        }

        public async Task<IDictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync()
        {
            await Task.Delay(1000);
            return managedObjects;
        }

        public void Dispose()
        {
            foreach (var proxies in managedObjects)
                foreach (var proxy in proxies.Value)
                    proxy.Dispose();
        }
    }
}
