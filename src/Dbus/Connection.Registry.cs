using System;
using System.Collections.Generic;

namespace Dbus
{
    public partial class Connection
    {
        private static Dictionary<Type, Func<Connection, object, ObjectPath, IDisposable>> publishFactories = new Dictionary<Type, Func<Connection, object, ObjectPath, IDisposable>>();

        public static void AddPublishProxy<T>(Func<Connection, T, ObjectPath, IDisposable> factory)
        {
            publishFactories.Add(typeof(T), (Connection connection, object target, ObjectPath path) =>
                factory(connection, (T)target, path)
            );
        }

        public IDisposable Publish<T>(T target, ObjectPath path = null)
        {
            return publishFactories[typeof(T)](this, target, path);
        }
    }
}
