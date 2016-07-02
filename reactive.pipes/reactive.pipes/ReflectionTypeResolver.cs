using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace reactive.pipes
{
    public class ReflectionTypeResolver : ITypeResolver
    {
        public IEnumerable<Type> GetDescendants(Type type)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = assemblies.SelectMany(a => a.GetTypes());
            IEnumerable<Type> descendants = types.Where(t => t.IsSubclassOf(type) || (type.IsAssignableFrom(t) && !t.IsInterface && t != type));
            return descendants;
        }

        public IEnumerable<Type> GetAncestors(Type type)
        {
            if (type?.BaseType == null || type.BaseType == typeof(object))
                yield break;

            foreach (var i in type.GetInterfaces())
                yield return i;
            
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }
    }
}