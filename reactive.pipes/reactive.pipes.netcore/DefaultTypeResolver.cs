using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace reactive.pipes
{
    public class DefaultTypeResolver : ITypeResolver
    {
        private readonly IEnumerable<Assembly> _assemblies;

        public DefaultTypeResolver(IEnumerable<Assembly> assemblies)
        {
            _assemblies = assemblies;
        }

        public Type FindTypeByName(string typeName)
        {
            IEnumerable<Type> loadedTypes =
                _assemblies.Where(a => a != typeof(object).GetTypeInfo().Assembly)
                    .SelectMany(a => a.GetTypes());

            Type type = loadedTypes.FirstOrDefault(t => t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

            return type;
        }

        public IEnumerable<Type> GetAncestors(Type type)
        {
            if (type?.GetTypeInfo().BaseType == null || type.GetTypeInfo().BaseType == typeof(object))
                yield break;

            foreach (var i in type.GetTypeInfo().GetInterfaces())
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