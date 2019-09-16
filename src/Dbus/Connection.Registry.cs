﻿using System;
using System.Collections.Generic;
using System.Threading;

namespace Dbus
{
    public partial class Connection
    {
        private static readonly Dictionary<Type, Func<Connection, object, ObjectPath?, IProxy>> publishFactories = new Dictionary<Type, Func<Connection, object, ObjectPath?, IProxy>>();
        private static readonly Dictionary<Type, Func<Connection, ObjectPath?, string?, CancellationToken, object>> consumeFactories = new Dictionary<Type, Func<Connection, ObjectPath?, string?, CancellationToken, object>>();

        public static void AddPublishProxy<T>(
            Func<Connection, T, ObjectPath?, IProxy> factory
        ) where T : notnull
            => publishFactories.Add(typeof(T), (Connection connection, object target, ObjectPath? path) =>
                factory(connection, (T)target, path)
            );

        public IProxy Publish<T>(
            T target,
            ObjectPath? path = null
        ) where T : notnull
            => publishFactories[typeof(T)](this, target, path);

        public static void AddConsumeImplementation<T>(
            Func<Connection, ObjectPath?, string?, CancellationToken, object> factory
        ) where T : notnull
            => consumeFactories.Add(typeof(T), factory);

        public T Consume<T>(
            ObjectPath? path = null,
            string? destination = null,
            CancellationToken cancellationToken = default
        ) where T : notnull
            => (T)consumeFactories[typeof(T)](this, path, destination, cancellationToken);
    }
}
