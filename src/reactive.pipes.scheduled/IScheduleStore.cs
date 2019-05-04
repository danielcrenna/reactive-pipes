// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace reactive.pipes.scheduled
{
	public interface IScheduleStore
	{
		IList<ScheduledTask> GetAll();
		IList<ScheduledTask> GetByAllTags(params string[] tags);
		IList<ScheduledTask> GetByAnyTags(params string[] tags);
		ScheduledTask GetById(int id);

		IList<ScheduledTask> GetHangingTasks();

		void Save(ScheduledTask task);
		void Delete(ScheduledTask task);
		IList<ScheduledTask> GetAndLockNextAvailable(int readAhead);
	}
}