using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus
{
    public abstract class OrgFreedesktopDbusObjectManager : IOrgFreedesktopDbusObjectManagerProvide
    {
        private readonly Connection connection;
        private readonly Dictionary<ObjectPath, List<IProxy>> managedObjects;
        private readonly IProxy thisProxy;
        private readonly SemaphoreSlim syncRoot = new SemaphoreSlim(1);

        protected OrgFreedesktopDbusObjectManager(Connection connection, ObjectPath root)
        {
            this.connection = connection;
            Root = root;
            managedObjects = new Dictionary<ObjectPath, List<IProxy>>();
            thisProxy = connection.Publish<IOrgFreedesktopDbusObjectManagerProvide>(this, Root);
        }

        public event Action<ObjectPath, IDictionary<string, IDictionary<string, object>>> InterfacesAdded { add { } remove { } }
        public event Action<ObjectPath, IEnumerable<string>> InterfacesRemoved { add { } remove { } }

        public ObjectPath Root { get; }

        public ObjectPath AddObject<TInterface>(TInterface instance, ObjectPath path = null)
        {
            var fullPath = buildFullPath(path);
            var proxy = connection.Publish(instance, fullPath);
            syncRoot.Wait();
            try
            {
                if (managedObjects.ContainsKey(fullPath))
                    managedObjects[fullPath].Add(proxy);
                else
                    managedObjects.Add(fullPath, new List<IProxy>() { proxy });
            }
            finally
            {
                syncRoot.Release();
            }
            return fullPath;
        }

        private ObjectPath buildFullPath(ObjectPath path)
        {
            if (path == null)
                return Root;

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

        public void RemoveObject<TInterface>(TInterface instance, ObjectPath path = null)
        {
            var fullPath = buildFullPath(path);
            IProxy removedProxy = null;
            syncRoot.Wait();
            try
            {
                if (managedObjects.TryGetValue(fullPath, out var proxies))
                {
                    var proxy = proxies.SingleOrDefault(x => ReferenceEquals(x.Target, instance));
                    proxies.Remove(proxy);
                    if (proxies.Count == 0)
                        managedObjects.Remove(fullPath);
                    removedProxy = proxy;
                }
            }
            finally
            {
                syncRoot.Release();
            }
            removedProxy?.Dispose();
        }

        public async Task<IDictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync(CancellationToken cancellationToken)
        {
            await syncRoot.WaitAsync();
            try
            {
                // Deep copy of the internal data structure.
                // That way the data structure can be modified
                // while the message is constructed
                return managedObjects.ToDictionary(x => x.Key, x => x.Value.ToList());
            }
            finally
            {
                syncRoot.Release();
            }
        }

        public void Dispose()
        {
            foreach (var proxies in managedObjects)
                foreach (var proxy in proxies.Value)
                    proxy.Dispose();
            thisProxy.Dispose();
            syncRoot.Dispose();
        }
    }
}
