using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace reactive.pipes
{
    public class DefaultTypeResolver : ITypeResolver
    {
        public IEnumerable<Type> GetAncestors(Type type)
        {
            if (type?.GetTypeInfo().BaseType == null || type.GetTypeInfo().BaseType == typeof(object))
                yield break;

            foreach (var i in type.GetInterfaces())
                yield return i;

            var baseType = type.GetTypeInfo().BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                yield return baseType;
                baseType = baseType.GetTypeInfo().BaseType;
            }
        }
    }
}