using System;
using System.Collections.Generic;

namespace reactive.pipes
{
    public interface ITypeResolver
    {
        IEnumerable<Type> GetAncestors(Type type);
        IEnumerable<Type> GetDescendants(Type type);
    }
}