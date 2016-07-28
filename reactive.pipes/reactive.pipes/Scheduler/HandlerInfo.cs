using System;
using System.Collections.Generic;

namespace reactive.pipes.Scheduler
{
    public class HandlerInfo : IEquatable<HandlerInfo>
    {
        public string Namespace { get; set; }
        public string Entrypoint { get; set; }
        public string Instance { get; set; }

        public HandlerInfo( /* Required for serialization */) { }

        public HandlerInfo(string @namespace, string entrypoint)
        {
            Namespace = @namespace;
            Entrypoint = entrypoint;
        }

        #region Equality

        public bool Equals(HandlerInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Namespace, other.Namespace) && string.Equals(Entrypoint, other.Entrypoint) && string.Equals(Instance, other.Instance);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((HandlerInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Namespace != null ? Namespace.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Entrypoint != null ? Entrypoint.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Instance != null ? Instance.GetHashCode() : 0);
                return hashCode;
            }
        }

        public static bool operator ==(HandlerInfo left, HandlerInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(HandlerInfo left, HandlerInfo right)
        {
            return !Equals(left, right);
        }

        private sealed class NamespaceEntrypointInstanceEqualityComparer : IEqualityComparer<HandlerInfo>
        {
            public bool Equals(HandlerInfo x, HandlerInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.Namespace, y.Namespace) && string.Equals(x.Entrypoint, y.Entrypoint) && string.Equals(x.Instance, y.Instance);
            }

            public int GetHashCode(HandlerInfo obj)
            {
                unchecked
                {
                    var hashCode = (obj.Namespace != null ? obj.Namespace.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.Entrypoint != null ? obj.Entrypoint.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.Instance != null ? obj.Instance.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        private static readonly IEqualityComparer<HandlerInfo> NamespaceEntrypointInstanceComparerInstance = new NamespaceEntrypointInstanceEqualityComparer();

        public static IEqualityComparer<HandlerInfo> NamespaceEntrypointInstanceComparer
        {
            get { return NamespaceEntrypointInstanceComparerInstance; }
        }

        #endregion
    }
}