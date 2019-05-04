// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace reactive.pipes.scheduled
{
	public static class ScheduledTaskExtensions
	{
		public static void RepeatIndefinitely(this ScheduledTask task, string expression)
		{
			task.Expression = expression;
		}

		public static void RepeatUntil(this ScheduledTask task, string expression, DateTimeOffset end)
		{
			task.Expression = expression;
			task.End = end;
		}
	}
}