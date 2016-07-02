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

            var handled = 0;
            aggregator.Subscribe<StringEvent>(se => { handled++; });

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.Equal(1, handled);
        }

        [Fact]
        public void Publishes_to_subscriber_by_topic()
        {
            var aggregator = new Hub();

            var handled = 0;
            aggregator.Subscribe<StringEvent>(se => { handled++; }, @event => @event.Text == "bababooey!");

            var sent = aggregator.Publish(new StringEvent("not bababooey!"));
            Assert.True(sent);
            Assert.Equal(0, handled);

            sent = aggregator.Publish(new StringEvent("bababooey!"));
            Assert.True(sent);
            Assert.Equal(1, handled);
        }

        [Fact]
        public void Publishes_to_handler()
        {
            var aggregator = new Hub();

            var handler = new StringEventHandler();
            aggregator.Subscribe(handler);

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.Equal(1, handler.Handled);
        }

        [Fact]
        public void Publishes_to_multiple_handlers()
        {
            var aggregator = new Hub();

            int handler1 = 0;
            int handler2 = 0;

            aggregator.Subscribe<StringEvent>(e => { handler1++; });
            aggregator.Subscribe<StringEvent>(e => { handler2++; });

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.Equal(1, handler1);
            Assert.Equal(1, handler2);
        }

        [Fact]
        public void Can_subscribe_with_manifold_consumer()
        {
            var aggregator = new Hub();

            ManifoldEventHandler handler = new ManifoldEventHandler();
            aggregator.Subscribe(handler);

            var sent = aggregator.Publish(new StringEvent("Foo"));
            Assert.True(sent);
            Assert.Equal(1, handler.HandledString);
            Assert.Equal(0, handler.HandledInteger);

            sent = aggregator.Publish(new IntegerEvent(123));
            Assert.True(sent);
            Assert.Equal(1, handler.HandledString);
            Assert.Equal(1, handler.HandledInteger);
        }

        [Fact]
        public void Can_subscribe_with_manifold_hierarchical_consumer()
        {
            var aggregator = new Hub();

            ManifoldHierarchicalEventHandler handler = new ManifoldHierarchicalEventHandler();
            aggregator.Subscribe(handler);

            var sent = aggregator.Publish(new InheritedEvent());
            Assert.True(sent);
            Assert.Equal(1, handler.HandledInterface);
            Assert.Equal(1, handler.HandledBase);
            Assert.Equal(1, handler.HandledInherited);
        }

        [Fact]
        public void Publishes_to_multicast_handlers()
        {
            var aggregator = new Hub();

            int baseCalled = 0;
            int inheritedCalled = 0;

            // we need to have a subscription from InheritedEvent to all BaseEvent handlers!
            Action<BaseEvent> handler1 = e => { baseCalled++; };
            Action<InheritedEvent> handler2 = e => { inheritedCalled++; };

            aggregator.Subscribe(handler1);
            aggregator.Subscribe(handler2);
        
            // one handler, many events (by virtue of class hierarchy)
            var sent = aggregator.Publish(new InheritedEvent { Id = 123, Value = "ABC" });
            Assert.True(sent);
            Assert.Equal(1, inheritedCalled);
            Assert.Equal(1, baseCalled);
        }

        [Fact]
        public void Publishes_to_multicast_handlers_with_no_existing_subscriptions()
        {
            int handled = 0;

            var hub = new Hub();
            hub.Subscribe<BaseEvent>(e => handled++);
            
            var sent = hub.Publish(new InheritedEvent { Id = 123, Value = "ABC" });
            Assert.True(sent, "did not send event to a known subscription");
            Assert.Equal(1, handled);
        }

        [Fact]
        public void Publishes_to_multicast_handlers_with_interfaces_with_delegate_consumer()
        {
            int handled = 0;

            var hub = new Hub();
            hub.Subscribe<IEvent>(e => handled++);

            var sent = hub.Publish(new InheritedEvent { Id = 123, Value = "ABC" });
            Assert.True(sent, "did not send event to a known subscription");
            Assert.Equal(1, handled);
        }

        [Fact]
        public void Publishes_to_multicast_handlers_with_interfaces_with_concrete_consumer()
        {
            var handler = new TestHandler();
            var hub = new Hub();
            hub.Subscribe(handler);
            hub.Publish(new InheritedEvent());
            Assert.Equal(1, handler.Handled);
        }
    }
}
