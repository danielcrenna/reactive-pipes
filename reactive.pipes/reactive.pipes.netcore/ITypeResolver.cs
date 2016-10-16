using System;
using System.Collections.Generic;

namespace reactive.pipes
{
    public interface ITypeResolver
    {
        Type FindTypeByName(string typeName);
        IEnumerable<Type> GetAncestors(Type type);
    }
}