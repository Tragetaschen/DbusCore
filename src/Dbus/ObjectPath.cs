using System;

namespace Dbus
{
    public class ObjectPath
    {
        private readonly string objectPath = "";

        private ObjectPath(string objectPath)
        {
            if (objectPath == null)
                throw new ArgumentNullException(nameof(objectPath));
            this.objectPath = objectPath;
        }

        public static implicit operator ObjectPath(string objectPath)
        {
            return new ObjectPath(objectPath);
        }

        public static readonly ObjectPath Empty = "";

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (ObjectPath)obj;
            return objectPath == other.objectPath;
        }

        public override int GetHashCode()
        {
            return objectPath.GetHashCode();
        }

        public override string ToString()
        {
            return objectPath;
        }

        public static bool operator ==(ObjectPath lhs, ObjectPath rhs)
        {
            return lhs.objectPath == rhs.objectPath;
        }

        public static bool operator !=(ObjectPath lhs, ObjectPath rhs)
        {
            return lhs.objectPath != rhs.objectPath;
        }
    }
}
