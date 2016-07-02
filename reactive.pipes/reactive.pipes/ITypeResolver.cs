using System;
using System.Collections.Generic;

namespace reactive.pipes
{
    public interface ITypeResolver
    {
        IEnumerable<Type> GetDescendants(Type type);
    }
}