// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace reactive.pipes.Extensions
{
	internal static class CollectionExtensions
	{
		public static IEnumerable<List<T>> Split<T>(this IEnumerable<T> source, int size)
		{
			var toReturn = new List<T>(size);
			foreach (var item in source)
			{
				toReturn.Add(item);
				if (toReturn.Count != size)
					continue;

				yield return toReturn;
				toReturn = new List<T>(size);
			}

			if (toReturn.Any()) yield return toReturn;
		}
	}
}