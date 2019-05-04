// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using Xunit;

namespace reactive.pipes.scheduled.tests
{
	public class OccurrenceTests
	{
		[Fact]
		public void Occurrence_is_in_UTC()
		{
			var task = new ScheduledTask();
			task.RunAt = DateTimeOffset.UtcNow;

			task.Expression = CronTemplates.Daily(1, 3, 30);
			var next = task.NextOccurrence;
			Assert.NotNull(next);
			Assert.True(next.Value.Hour == 3);
			Assert.Equal(next.Value.Hour, next.Value.UtcDateTime.Hour);
		}
	}
}