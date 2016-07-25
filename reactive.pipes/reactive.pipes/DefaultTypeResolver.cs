using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace reactive.pipes
{
    public class DefaultTypeResolver : ITypeResolver
    {
        public Type FindTypeByName(string typeName)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            IEnumerable<Type> loadedTypes =
                assemblies.Where(a => a != typeof(object).Assembly)
                    .SelectMany(a => a.GetTypes());

            Type type = loadedTypes.FirstOrDefault(t => t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            return type;
        }

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