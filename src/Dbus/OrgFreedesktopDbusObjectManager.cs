using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Dbus;

public class OrgFreedesktopDbusObjectManager : IOrgFreedesktopDbusObjectManagerProvide
{
    private readonly Connection connection;
    private readonly Dictionary<ObjectPath, List<IProxy>> managedObjects = [];
    private readonly IProxy thisProxy;
    private readonly SemaphoreSlim syncRoot = new(1);

    public OrgFreedesktopDbusObjectManager(Connection connection, ObjectPath root)
    {
        this.connection = connection;
        Root = root;
        thisProxy = connection.Publish<IOrgFreedesktopDbusObjectManagerProvide>(this, Root);
    }

    public event Action<ObjectPath, Dictionary<string, Dictionary<string, object>>> InterfacesAdded { add { } remove { } }
    public event Action<ObjectPath, List<string>> InterfacesRemoved { add { } remove { } }

    public ObjectPath Root { get; }

    public ObjectPath AddObject<TInterface>(TInterface instance, ObjectPath? path = null)
         where TInterface : class
    {
        var fullPath = buildFullPath(path);
        var proxy = connection.Publish(instance, fullPath);
        syncRoot.Wait();
        try
        {
            if (managedObjects.ContainsKey(fullPath))
                managedObjects[fullPath].Add(proxy);
            else
                managedObjects.Add(fullPath, [proxy]);
        }
        finally
        {
            syncRoot.Release();
        }
        return fullPath;
    }

    private ObjectPath buildFullPath(ObjectPath? path)
    {
        if (path is null)
            return Root;

        var rootString = Root.ToString();
        var pathString = path.ToString();
        if (pathString.StartsWith(rootString))
            return path;
        if (!pathString.StartsWith("./"))
            throw new ArgumentException("A partial path has to start with ./");
        if (rootString == "/")
            return pathString[1..];
        if (pathString == "./")
            return rootString;
        return rootString + pathString[1..];
    }

    public void RemoveObject<TInterface>(TInterface instance, ObjectPath? path = null)
        where TInterface : class
    {
        var fullPath = buildFullPath(path);
        IProxy? removedProxy = null;
        syncRoot.Wait();
        try
        {
            if (managedObjects.TryGetValue(fullPath, out var proxies))
            {
                foreach (var proxy in proxies)
                    if (ReferenceEquals(proxy.Target, instance))
                    {
                        removedProxy = proxy;
                        break;
                    }
                if (removedProxy != null)
                {
                    proxies.Remove(removedProxy);
                    if (proxies.Count == 0)
                        managedObjects.Remove(fullPath);
                }
            }
        }
        finally
        {
            syncRoot.Release();
        }
        removedProxy?.Dispose();
    }

    public async Task<Dictionary<ObjectPath, List<IProxy>>> GetManagedObjectsAsync(CancellationToken cancellationToken)
    {
        await syncRoot.WaitAsync(cancellationToken);
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
