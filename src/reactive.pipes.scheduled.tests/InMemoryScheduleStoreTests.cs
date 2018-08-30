namespace reactive.pipes.scheduled.tests
{
    public class InMemoryScheduleStoreTests : ScheduleStoreTests
    {
        public InMemoryScheduleStoreTests()
        {
            Store = new InMemoryScheduleStore();
        }
    }
}