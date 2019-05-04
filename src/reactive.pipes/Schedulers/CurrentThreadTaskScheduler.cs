// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;

namespace System.Threading.Tasks.Schedulers
{
	/// <summary>Provides a task scheduler that runs tasks on the current thread.</summary>
	public sealed class CurrentThreadTaskScheduler : TaskScheduler
	{
		/// <summary>Gets the maximum degree of parallelism for this scheduler.</summary>
		public override int MaximumConcurrencyLevel => 1;

		/// <summary>Runs the provided Task synchronously on the current thread.</summary>
		/// <param name="task">The task to be executed.</param>
		protected override void QueueTask(Task task)
		{
			TryExecuteTask(task);
		}

		/// <summary>Runs the provided Task synchronously on the current thread.</summary>
		/// <param name="task">The task to be executed.</param>
		/// <param name="taskWasPreviouslyQueued">Whether the Task was previously queued to the scheduler.</param>
		/// <returns>True if the Task was successfully executed; otherwise, false.</returns>
		protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
		{
			return TryExecuteTask(task);
		}

		/// <summary>Gets the Tasks currently scheduled to this scheduler.</summary>
		/// <returns>An empty enumerable, as Tasks are never queued, only executed.</returns>
		protected override IEnumerable<Task> GetScheduledTasks()
		{
			return Enumerable.Empty<Task>();
		}
	}
}