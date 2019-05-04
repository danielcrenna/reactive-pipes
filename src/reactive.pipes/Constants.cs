// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Reflection;

namespace reactive.pipes
{
	internal static class Constants
	{
		public static class Methods
		{
			public static readonly MethodInfo PublishTyped =
				typeof(Hub).GetMethod(nameof(PublishTyped), BindingFlags.NonPublic | BindingFlags.Instance);
		}
	}
}