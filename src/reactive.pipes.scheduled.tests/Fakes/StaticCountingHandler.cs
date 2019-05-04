// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes.scheduled.tests.Fakes
{
	public class StaticCountingHandler
	{
		public static int Count { get; set; }

		public string SomeOption { get; set; }

		public bool Perform()
		{
			Count++;
			return true;
		}
	}
}