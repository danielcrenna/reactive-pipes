using System;
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

        [Fact]
        public void Can_handle_error_with_callback()
        {
            var errors = 0;

            var hub = new Hub();

            bool sent = false;

            // SubscribeWithDelegate:
            {
                hub.Subscribe<InheritedEvent>(e => { throw new Exception(); }, ex => { errors++; });
                object @event = new InheritedEvent { Id = 123, Value = "ABC" };
                sent = hub.Publish(@event);
                Assert.False(sent, "publishing a failed event should bubble as false to the publish result");
                Assert.Equal(1, errors);
            }

            // SubscribeWithDelegateAndTopic:
            {
                hub.Subscribe<StringEvent>(se => { throw new Exception(); }, @event => @event.Text == "bababooey!", ex => { errors++; });
                sent = hub.Publish(new StringEvent("not bababooey!"));
                Assert.False(sent);
                Assert.Equal(1, errors);
                sent = hub.Publish(new StringEvent("bababooey!"));
                Assert.False(sent);
                Assert.Equal(2, errors);
            }

            // Subscribe (manifold):
            {
                ManifoldHierarchicalEventHandler handler = new ManifoldHierarchicalEventHandler();
                hub.Subscribe(handler, ex => { errors++; });
                sent = hub.Publish(new ErrorEvent());
                Assert.False(sent);
                Assert.Equal(3, errors);
            }

            // SubscribeWithInterface:
            {
                hub.Subscribe(new ErroringHandler());
                sent = hub.Publish(new ErrorEvent());
                Assert.False(sent);
                Assert.Equal(4, errors);
            }
        }
    }
}