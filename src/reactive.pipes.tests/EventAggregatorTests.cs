using System;
using System.Collections.Generic;
using System.Threading;
using reactive.pipes.tests.Fakes;
using reactive.tests.Fakes;
using Xunit;

namespace reactive.pipes.tests
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
			Assert.False(sent);
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
		public void Can_subscribe_with_multiple_hierarchical_consumers()
		{
			var aggregator = new Hub();

			ManifoldHierarchicalEventHandler handler1 = new ManifoldHierarchicalEventHandler();
			ManifoldHierarchicalEventHandler handler2 = new ManifoldHierarchicalEventHandler();
			ManifoldHierarchicalEventHandler handler3 = new ManifoldHierarchicalEventHandler();

			aggregator.Subscribe<IEvent>(handler1);
			aggregator.Subscribe<BaseEvent>(handler2);
			aggregator.Subscribe<InheritedEvent>(handler3);

			var sent = aggregator.Publish(new InheritedEvent());
			Assert.True(sent);
			Assert.Equal(1, handler1.HandledInterface);
			Assert.Equal(0, handler1.HandledBase);
			Assert.Equal(0, handler1.HandledInherited);

			Assert.Equal(0, handler2.HandledInterface);
			Assert.Equal(1, handler2.HandledBase);
			Assert.Equal(0, handler2.HandledInherited);

			Assert.Equal(0, handler3.HandledInterface);
			Assert.Equal(0, handler3.HandledBase);
			Assert.Equal(1, handler3.HandledInherited);
		}

		[Fact]
		public void Same_consumer_will_receive_duplicates()
		{
			// Important: the hub does not track references to handlers, so subscribing twice means you get two messages!

			var handler1 = new SuccessHandler();
			var aggregator = new Hub();
			aggregator.Subscribe(handler1);
			aggregator.Subscribe(handler1);
			var sent = aggregator.Publish(new InheritedEvent {Id = 123, Value = "ABC"});
			Assert.True(sent);
			Assert.Equal(2, handler1.Handled);
		}

		[Fact]
		public void Multiple_subscriptions_of_the_same_kind_dont_duplicate()
		{
			var handler1 = new SuccessHandler();
			var handler2 = new SuccessHandler();
			var handled = 0;

			var aggregator = new Hub();
			aggregator.Subscribe<IEvent>(handler1);
			aggregator.Subscribe<IEvent>(e => handled++);
			aggregator.Subscribe<IEvent>(handler2);

			var sent = aggregator.Publish(new InheritedEvent {Id = 123, Value = "ABC"});
			Assert.True(sent);
			Assert.Equal(1, handler1.Handled);
			Assert.Equal(1, handled);
			Assert.Equal(1, handler2.Handled);
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
			var sent = aggregator.Publish(new InheritedEvent {Id = 123, Value = "ABC"});
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

			var sent = hub.Publish(new InheritedEvent {Id = 123, Value = "ABC"});
			Assert.True(sent, "did not send event to a known subscription");
			Assert.Equal(1, handled);
		}

		[Fact]
		public void Publishes_to_multicast_handlers_with_interfaces_with_delegate_consumer()
		{
			int handled = 0;

			var hub = new Hub();

			Action<IEvent> handler = e => handled++;
			hub.Subscribe(handler);

			var sent = hub.Publish(new InheritedEvent {Id = 123, Value = "ABC"});
			Assert.True(sent, "did not send event to a known subscription");
			Assert.Equal(1, handled);
		}

		[Fact]
		public void Publishes_to_multicast_handlers_with_interfaces_with_concrete_consumer()
		{
			var handler = new SuccessHandler();
			var hub = new Hub();
			hub.Subscribe(handler);
			hub.Publish(new InheritedEvent());
			Assert.Equal(1, handler.Handled);
		}

		[Fact]
		public void Handlers_survive_exceptions()
		{
			var handler = new ThrowingHandler();
			var hub = new Hub();
			hub.Subscribe(handler);

			hub.Publish(new InheritedEvent());
			Assert.Equal(1, handler.Handled);

			hub.Publish(new InheritedEvent());
			Assert.Equal(2, handler.Handled);
		}

		[Fact]
		public void Handlers_can_return_false_safely()
		{
			var handler = new FailingHandler();
			var hub = new Hub();
			hub.Subscribe(handler);
			bool result = hub.Publish(new InheritedEvent());
			Assert.Equal(1, handler.Handled);
			Assert.False(result);
		}

		[Fact]
		public void Handlers_can_return_true_safely()
		{
			var handler = new SuccessHandler();
			var hub = new Hub();
			hub.Subscribe(handler);
			bool result = hub.Publish(new InheritedEvent());
			Assert.Equal(1, handler.Handled);
			Assert.True(result);
		}

		[Fact]
		public void Synchronous_publish_works_with_asynchronous_handler()
		{
			var handler = new LongRunningAsyncHandler();
			var hub = new Hub();
			hub.Subscribe(handler);
			bool result = hub.Publish(new InheritedEvent());
			Assert.Equal(1, handler.Handled);
			Assert.True(result);
		}

		[Fact]
		public void Two_handlers_for_the_same_event_type()
		{
			var hub = new Hub();
			var h1 = new StringEventHandler();
			var h2 = new StringEventHandler2();
			hub.Subscribe((IConsume<StringEvent>)h1);
			hub.Subscribe((IConsume<StringEvent>)h2);
			bool result = hub.Publish(new StringEvent("value"));
			Assert.True(h1.Handled == 1);
			Assert.True(h2.Handled == 1);
			Assert.True(result);
		}

		[Fact]
		public void Two_identical_handlers()
		{
			var hub = new Hub();
			var h1 = new StringEventHandler();
			var h2 = new StringEventHandler();
			hub.Subscribe((IConsume<StringEvent>)h1);
			hub.Subscribe((IConsume<StringEvent>)h2);
			bool result = hub.Publish(new StringEvent("value"));
			Assert.True(h1.Handled == 1);
			Assert.True(h2.Handled == 1);
			Assert.True(result);
		}

		[Fact]
		public void Same_instance_subscribed_twice()
		{
			var hub = new Hub();
			var h1 = new StringEventHandler();
			hub.Subscribe((IConsume<StringEvent>)h1);
			hub.Subscribe((IConsume<StringEvent>)h1);
			bool result = hub.Publish(new StringEvent("value"));
			Assert.True(h1.Handled == 2);
			Assert.True(result);
		}

		[Fact]
		public void Two_identical_closure_handlers()
		{
			int handled1 = 0;
			int handled2 = 0;
			var hub = new Hub();
			hub.Subscribe(new Action<StringEvent>(x => handled1++));
			hub.Subscribe(new Action<StringEvent>(x => handled2++));
			bool result = hub.Publish(new StringEvent("value"));
			Assert.True(handled1 == 1);
			Assert.True(handled2 == 1);
			Assert.True(result);
		}

		[Fact]
		public void Same_instance_through_manifold()
		{	
			var hub = new Hub();
			var h = new StringEventHandler();
			hub.Subscribe((object)h);
			hub.Subscribe((object)h);
			bool result = hub.Publish(new StringEvent("value"));
			Assert.Equal(2, h.Handled);
			Assert.True(result);
		}

		[Fact]
		public void Can_use_scoped_handler()
		{
			var hub = new Hub();
			var cache = new ThreadLocal<List<string>>(()=> new List<string>());
			var handler = new ThreadLocalScopedHandler(cache);
			hub.Subscribe(handler);
			bool result = hub.Publish(new BaseEvent());
			Assert.True(result);
			Assert.Single(handler.Lines); // Before

			var accessor = cache;
			Assert.NotNull(accessor.Value);
			Assert.Equal(2, accessor.Value.Count); // Before, After
		}
	}
}
