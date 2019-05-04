// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Xunit;

namespace reactive.pipes.scheduled.tests
{
	public abstract class ScheduleStoreTests
	{
		protected IScheduleStore Store;

		private static ScheduledTask CreateNewTask()
		{
			var task = new ScheduledTask();
			var settings = new ScheduledProducerSettings();

			// these values are required and must be set by implementation
			task.Handler = "{}";
			task.MaximumAttempts = settings.MaximumAttempts;
			task.MaximumRuntime = settings.MaximumRuntime;
			task.DeleteOnError = settings.DeleteOnError;
			task.DeleteOnFailure = settings.DeleteOnFailure;
			task.DeleteOnSuccess = settings.DeleteOnSuccess;

			return task;
		}

		[Fact]
		public void Adding_tags_synchronizes_with_store()
		{
			var created = CreateNewTask();
			created.Tags.Add("a");
			Store.Save(created);

			created.Tags.Add("b");
			Store.Save(created);

			created.Tags.Add("c");
			Store.Save(created);

			// GetAll:
			var all = Store.GetAll();
			Assert.Equal(1, all.Count);
			Assert.Equal(3, all[0].Tags.Count);
		}

		[Fact]
		public void Can_delete_tasks_with_tags()
		{
			var created = CreateNewTask();
			created.Tags.Add("a");
			created.Tags.Add("b");
			created.Tags.Add("c");
			Store.Save(created);

			Store.Delete(created);
		}

		[Fact]
		public void Can_save_multiple_tasks_with_tags()
		{
			var first = CreateNewTask();
			first.Tags.Add("one");
			Store.Save(first);

			var second = CreateNewTask();
			second.Tags.Add("two");
			Store.Save(second);

			var tasks = Store.GetByAllTags("one");
			Assert.True(tasks.Count == 1);
			Assert.True(tasks[0].Tags.Count == 1);

			Store.Delete(second);
		}

		[Fact]
		public void Can_search_for_all_tags()
		{
			var created = CreateNewTask();
			created.Tags.Add("a");
			created.Tags.Add("b");
			created.Tags.Add("c");
			Store.Save(created);

			// GetByAllTags (miss):
			var all = Store.GetByAllTags("a", "b", "c", "d");
			Assert.Equal(0, all.Count);

			// GetByAnyTags (hit):
			all = Store.GetByAllTags("a", "b", "c");
			Assert.Equal(1, all.Count);
			Assert.Equal(3, all[0].Tags.Count);
		}

		[Fact]
		public void Can_search_for_any_tags()
		{
			var created = CreateNewTask();
			created.Tags.Add("a");
			created.Tags.Add("b");
			created.Tags.Add("c");
			Store.Save(created);

			// GetByAllTags (miss):
			var all = Store.GetByAnyTags("e");
			Assert.Equal(0, all.Count);

			// GetByAnyTags (hit):
			all = Store.GetByAnyTags("e", "a");
			Assert.Equal(1, all.Count);
			Assert.Equal(3, all[0].Tags.Count);
		}

		[Fact]
		public void Inserts_new_task()
		{
			var created = CreateNewTask();

			Assert.True(created.Id == 0);
			Store.Save(created);
			Assert.False(created.Id == 0);

			var loaded = Store.GetById(created.Id);
			Assert.NotNull(loaded);
			Assert.Equal(created.Id, loaded.Id);
		}

		[Fact]
		public void Locked_tasks_are_not_visible_to_future_fetches()
		{
			var created = CreateNewTask();

			Store.Save(created);

			var locked = Store.GetAndLockNextAvailable(int.MaxValue);
			Assert.False(locked.Count == 0, "did not retrieve at least one unlocked task");

			locked = Store.GetAndLockNextAvailable(int.MaxValue);
			Assert.True(locked.Count == 0, "there was at least one unlocked task after locking all of them");
		}

		[Fact]
		public void Removing_tags_synchronizes_with_store()
		{
			var created = CreateNewTask();
			created.Tags.Add("a");
			created.Tags.Add("b");
			created.Tags.Add("c");
			Store.Save(created);

			created.Tags.Remove("a");
			Store.Save(created);

			// GetAll:
			var all = Store.GetAll();
			Assert.Equal(1, all.Count);
			Assert.Equal(2, all[0].Tags.Count);

			// GetById:
			var byId = Store.GetById(1);
			Assert.NotNull(byId);
			Assert.Equal(2, byId.Tags.Count);

			created.Tags.Clear();
			Store.Save(created);

			// GetById:
			byId = Store.GetById(1);
			Assert.NotNull(byId);
			Assert.True(byId.Tags.Count == 0);
		}

		[Fact]
		public void Tags_are_saved_with_tasks()
		{
			var created = CreateNewTask();
			created.Tags.Add("a");
			created.Tags.Add("b");
			created.Tags.Add("c");
			Store.Save(created);

			// GetAll:
			var all = Store.GetAll();
			Assert.Equal(1, all.Count);
			Assert.Equal(3, all[0].Tags.Count);

			// GetById:
			var byId = Store.GetById(1);
			Assert.NotNull(byId);
			Assert.Equal(3, byId.Tags.Count);

			// GetAndLockNextAvailable:
			var locked = Store.GetAndLockNextAvailable(1);
			Assert.Equal(1, locked.Count);
			Assert.Equal(3, locked[0].Tags.Count);
		}
	}
}