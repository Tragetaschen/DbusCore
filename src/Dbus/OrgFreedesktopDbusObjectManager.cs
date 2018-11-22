using System;
using System.Collections.Generic;
using System.Threading;
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

        public ObjectPath AddObject<TInterface, TImplementation>(TImplementation instance, ObjectPath path) where TImplementation : TInterface
        {
            var fullPath = buildFullPath(path);
            var proxy = (IProxy)connection.Publish<TInterface>(instance, fullPath);
            lock (managedObjects)
            {
                if (managedObjects.ContainsKey(fullPath))
                    managedObjects[fullPath].Add(proxy);
                else
                    managedObjects.Add(fullPath, new List<IProxy>() { proxy });
            }
            return fullPath;
        }

        private ObjectPath buildFullPath(ObjectPath path)
        {
            var rootString = Root.ToString();
            var pathString = path.ToString();
            if (pathString.StartsWith(rootString))
                return path;
            if (!pathString.StartsWith("./"))
                throw new ArgumentException("A partial path has to start with ./");
            if (rootString == "/")
                return pathString.Substring(1);
            if (pathString == "./")
                return rootString;
            return rootString + pathString.Substring(1);
        }

        public virtual Task<IDictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync(CancellationToken cancellationToken)
            => Task.FromResult<IDictionary<ObjectPath, List<IProxy>>>(managedObjects);

        public void Dispose()
        {
            foreach (var proxies in managedObjects)
                foreach (var proxy in proxies.Value)
                    proxy.Dispose();
        }
    }
}
