using System.Threading;
using reactive.pipes;
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
    }
}
