// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace reactive.pipes
{
	public class DefaultTypeResolver : ITypeResolver
	{
		private readonly IEnumerable<Assembly> _assemblies;

		public DefaultTypeResolver(IEnumerable<Assembly> assemblies) => _assemblies = assemblies;

		public DefaultTypeResolver() : this(AppDomain.CurrentDomain.GetAssemblies())
		{
		}

		public Type FindTypeByName(string typeName)
		{
			var loadedTypes =
				_assemblies.Where(a => a != typeof(object).GetTypeInfo().Assembly)
					.SelectMany(a => a.GetTypes());

			var type = loadedTypes.FirstOrDefault(t => t.FullName.Equals(typeName, StringComparison.OrdinalIgnoreCase));

			return type;
		}

		public IEnumerable<Type> GetAncestors(Type type)
		{
			foreach (var i in type?.GetTypeInfo()?.GetInterfaces() ?? Enumerable.Empty<Type>())
				yield return i;

			if (type?.GetTypeInfo().BaseType == null || type.GetTypeInfo().BaseType == typeof(object))
				yield break;

			var baseType = type.GetTypeInfo().BaseType;
			while (baseType != null && baseType != typeof(object))
			{
				yield return baseType;
				baseType = baseType.GetTypeInfo().BaseType;
			}
		}
	}
}