using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace reactive.pipes
{
    public class AppDomainTypeResolver : ITypeResolver
    {
        public IEnumerable<Type> GetDescendants(Type type)
        {
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            IEnumerable<Type> types = assemblies.SelectMany(a => a.GetTypes());
            IEnumerable<Type> siblings = types.Where(t => t.IsSubclassOf(type) || (type.IsAssignableFrom(t) && !t.IsInterface && t != type));
            return siblings;
        }
    }
}