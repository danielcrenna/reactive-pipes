using reactive.pipes;
using reactive.tests.Fakes;
using Xunit;

namespace reactive.tests
{
    public class EventPublisherTests
    {
        [Fact]
        public void Can_publish_events_by_type()
        {
            var handled = 0;
            var hub = new Hub();
            hub.Subscribe<InheritedEvent>(e => handled++);
            object @event = new InheritedEvent {Id = 123, Value = "ABC"};
            var sent = hub.Publish(@event);

            Assert.True(sent, "did not send event to a known subscription");
            Assert.Equal(1, handled);
        }
    }
}