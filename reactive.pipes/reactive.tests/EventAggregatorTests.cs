using System;
using reactive.pipes;
using reactive.tests.Fakes;
using Xunit;

namespace reactive.tests
{
    public class EventAggregatorTests
    {
        [Fact]
        public void Publishes_to_simple_subscriber()
        {
            var aggregator = new Hub();

            var handled = false;
            aggregator.Subscribe<StringEvent>(se => { handled = true; });

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.True(handled);
        }

        [Fact]
        public void Publishes_to_subscriber_by_topic()
        {
            var aggregator = new Hub();

            var handled = false;
            aggregator.Subscribe<StringEvent>(se => { handled = true; }, @event => @event.Text == "bababooey!");

            var sent = aggregator.Publish(new StringEvent("not bababooey!"));
            Assert.True(sent);
            Assert.False(handled);

            sent = aggregator.Publish(new StringEvent("bababooey!"));
            Assert.True(sent);
            Assert.True(handled);
        }

        [Fact]
        public void Publishes_to_handler()
        {
            var aggregator = new Hub();

            var handler = new StringEventHandler();
            aggregator.Subscribe(handler);

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.True(handler.Handled);
        }

        [Fact]
        public void Publishes_to_multiple_handlers()
        {
            var aggregator = new Hub();

            bool handler1 = false;
            bool handler2 = false;

            aggregator.Subscribe<StringEvent>(e => { handler1 = true; });
            aggregator.Subscribe<StringEvent>(e => { handler2 = true; });

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.True(handler1);
            Assert.True(handler2);
        }

        [Fact]
        public void Can_subscribe_with_manifold_consumer()
        {
            var aggregator = new Hub();

            ManifoldEventHandler handler = new ManifoldEventHandler();
            aggregator.Subscribe(handler);

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.True(handler.HandledString, "string event was not handled");
            Assert.False(handler.HandledInteger, "integer event was improperly handled");

            sent = aggregator.Publish(new IntegerEvent(123));
            Assert.True(sent);
            Assert.True(handler.HandledString, "string event handling result was lost");
            Assert.True(handler.HandledInteger, "integer event was not handled");
        }

        [Fact]
        public void Publishes_to_multicast_handlers()
        {
            var aggregator = new Hub();

            bool baseCalled = false;
            bool inheritedCalled = false;

            // we need to have a subscription from InheritedEvent to all BaseEvent handlers!
            Action<BaseEvent> handler1 = e => { baseCalled = true; };
            Action<InheritedEvent> handler2 = e => { inheritedCalled = true; };

            aggregator.Subscribe(handler1);
            aggregator.Subscribe(handler2);
        
            // one handler, many events (by virtue of class hierarchy)
            var sent = aggregator.Publish(new InheritedEvent { Id = 123, Value = "ABC" });
            Assert.True(sent);
            Assert.True(inheritedCalled, "did not handle highest level event");
            Assert.True(baseCalled, "did not handle lowest level event");
        }
    }
}
