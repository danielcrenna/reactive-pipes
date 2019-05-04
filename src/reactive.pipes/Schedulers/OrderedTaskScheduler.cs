// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace System.Threading.Tasks.Schedulers
{
	/// <summary>
	///     Provides a task scheduler that ensures only one task is executing at a time, and that tasks
	///     execute in the order that they were queued.
	/// </summary>
	public sealed class OrderedTaskScheduler : LimitedConcurrencyLevelTaskScheduler
	{
		/// <summary>Initializes an instance of the OrderedTaskScheduler class.</summary>
		public OrderedTaskScheduler() : base(1)
		{
		}
	}
}