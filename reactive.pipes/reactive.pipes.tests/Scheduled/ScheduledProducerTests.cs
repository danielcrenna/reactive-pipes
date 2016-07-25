using System;
using System.Threading;
using System.Threading.Tasks;
using reactive.pipes.scheduled;
using reactive.tests.Scheduled.Fakes;
using Xunit;

namespace reactive.tests.Scheduled
{
    public class ScheduledProducerTests
    {
        [Fact]
        public void Starts_and_stops()
        {
            ScheduledProducer scheduler = new ScheduledProducer();
            scheduler.Start();
            scheduler.Stop();
        }

        [Fact]
        public void Queues_for_immediate_execution()
        {
            ScheduledProducerSettings settings = new ScheduledProducerSettings { DelayTasks = false };
            ScheduledProducer scheduler = new ScheduledProducer(settings);
            scheduler.ScheduleAsync<CountingHandler>();

            Assert.True(CountingHandler.Count == 1, "handler should have queued immediately since tasks are not delayed");
        }

        [Fact]
        public void Queues_for_delated_execution()
        {
            ScheduledProducerSettings settings = new ScheduledProducerSettings
            {
                DelayTasks = true,
                SleepInterval = TimeSpan.FromMilliseconds(100)
            };

            ScheduledProducer scheduler = new ScheduledProducer(settings);
            scheduler.ScheduleAsync<CountingHandler>(runAt: DateTimeOffset.UtcNow + TimeSpan.FromMilliseconds(300));
            scheduler.Start(); // <-- starts background thread to poll for tasks

            Assert.True(CountingHandler.Count == 0, "handler should not have queued immediately since tasks are delayed");
            Thread.Sleep(1000); // <-- should poll for tasks about 10 times
            Assert.True(CountingHandler.Count > 0, "handler should have executed since we scheduled it in the future");
            Assert.True(CountingHandler.Count == 1, "handler should have only executed once since it does not repeat");
        }
    }
}
