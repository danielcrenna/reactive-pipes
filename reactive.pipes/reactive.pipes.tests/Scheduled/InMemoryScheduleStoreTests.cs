using reactive.pipes.Scheduler;

namespace reactive.tests.Scheduled
{
    public class InMemoryScheduleStoreTests : ScheduleStoreTests
    {
        public InMemoryScheduleStoreTests()
        {
            Store = new InMemoryScheduleStore();
        }
    }
}