using reactive.pipes.scheduled;

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