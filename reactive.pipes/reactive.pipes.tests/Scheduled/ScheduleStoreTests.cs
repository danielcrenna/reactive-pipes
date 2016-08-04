using reactive.pipes.Scheduler;
using Xunit;

namespace reactive.tests.Scheduled
{
    public abstract class ScheduleStoreTests
    {
        protected IScheduleStore Store;

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
    }
}