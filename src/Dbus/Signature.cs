using System;

namespace Dbus
{
    public class Signature
    {
        private readonly string signature = "";

        private Signature(string signature)
        {
            if (signature == null)
                throw new ArgumentNullException(nameof(signature));
            this.signature = signature;
        }

        public static implicit operator Signature(string signature) =>
            new Signature(signature);

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (Signature)obj;
            return signature == other.signature;
        }

        public override int GetHashCode() => signature.GetHashCode();
        public override string ToString() => signature;
        public static bool operator ==(Signature lhs, Signature rhs) =>
            lhs.signature == rhs.signature;
        public static bool operator !=(Signature lhs, Signature rhs) =>
            lhs.signature != rhs.signature;
    }
}
