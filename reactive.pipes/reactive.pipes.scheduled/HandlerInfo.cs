using System;
using System.Collections.Generic;

namespace reactive.pipes.scheduled
{
    public class HandlerInfo : IEquatable<HandlerInfo>
    {
        public string Namespace { get; set; }
        public string Entrypoint { get; set; }

        public HandlerInfo( /* Required for serialization */) { }

        public HandlerInfo(string @namespace, string entrypoint)
        {
            Namespace = @namespace;
            Entrypoint = entrypoint;
        }

        private sealed class NamespaceEntrypointEqualityComparer : IEqualityComparer<HandlerInfo>
        {
            public bool Equals(HandlerInfo x, HandlerInfo y)
            {
                return string.Equals(x.Namespace, y.Namespace, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Entrypoint, y.Entrypoint, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(HandlerInfo obj)
            {
                unchecked
                {
                    return ((obj.Namespace != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Namespace) : 0)*397) ^ (obj.Entrypoint != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(obj.Entrypoint) : 0);
                }
            }
        }

        public bool Equals(HandlerInfo other)
        {
            return string.Equals(Namespace, other.Namespace, StringComparison.OrdinalIgnoreCase) && string.Equals(Entrypoint, other.Entrypoint, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is HandlerInfo && Equals((HandlerInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Namespace != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Namespace) : 0)*397) ^ (Entrypoint != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(Entrypoint) : 0);
            }
        }

        public static bool operator ==(HandlerInfo left, HandlerInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(HandlerInfo left, HandlerInfo right)
        {
            return !left.Equals(right);
        }

        private static readonly IEqualityComparer<HandlerInfo> NamespaceEntrypointComparerInstance = new NamespaceEntrypointEqualityComparer();

        public static IEqualityComparer<HandlerInfo> NamespaceEntrypointComparer
        {
            get { return NamespaceEntrypointComparerInstance; }
        }
    }
}