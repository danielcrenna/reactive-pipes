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
            bool handled = false;
            var hub = new Hub();
            hub.Subscribe<InheritedEvent>(e => handled = true);
            object @event = new InheritedEvent {Id = 123, Value = "ABC"};
            var sent = hub.Publish(@event);

            Assert.True(sent, "did not send event to a known subscription");
            Assert.True(handled, "did not handle lowest level event");
        }
    }
}