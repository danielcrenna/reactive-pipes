using System;
using System.Threading;
using System.Threading.Tasks;
using reactive.pipes.Producers;
using reactive.pipes.tests.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace reactive.pipes.tests
{
	public class BackgroundThreadProducerTests
	{
		readonly ITestOutputHelper _console;

		public BackgroundThreadProducerTests(ITestOutputHelper console)
		{
			_console = console;
		}

		[Fact]
		public async Task Can_deliver_single_batch_of_messages_fully()
		{
			const int expected = 1000;
			var actual = 0;

			// we have ten background workers trying to consume messages from an internal buffer
			var producer = new BackgroundThreadProducer<BaseEvent> {MaxDegreeOfParallelism = 10};
			producer.Attach(x => { Interlocked.Increment(ref actual); });
			await producer.Start();

			// while those workers are waiting for new messages, we feed the production
			for (var i = 0; i < expected; i++)
				await producer.Produce(new BaseEvent {Id = i});

			// then, we stop the service in non-immediate mode;
			// this should result in all the messages being sent since
			// we clear the buffer before shutting down delivery
			await producer.Stop(immediate: false);

			while (actual != expected)
				await Task.Delay(10);
		}

		[Fact]
		public async Task Can_deliver_single_batch_of_messages_with_overrun_in_backlog()
		{
			const int expected = 1000;
			var actual = 0;
			var backlogged = 0;

			// we have ten background workers trying to consume messages from an internal buffer
			var producer = new BackgroundThreadProducer<BaseEvent> {MaxDegreeOfParallelism = 10};

			producer.AttachBacklog(x => Interlocked.Increment(ref backlogged));
			producer.Attach(x => { Interlocked.Increment(ref actual); });
			await producer.Start();

			// while those workers are waiting for new messages, we feed the production
			for (var i = 0; i < expected; i++)
				await producer.Produce(new BaseEvent {Id = i});

			// then, we stop the service in immediate mode;
			// this should result in all undelivered messages redirecting to the backlog consumer
			// before shutting down delivery
			await producer.Stop(immediate: true);

			while (actual + backlogged != expected)
				await Task.Delay(10);

			_console.WriteLine($"{actual} sent");
			_console.WriteLine($"{backlogged} backlogged");
		}

		[Fact]
		public async Task Can_track_delivery_rate()
		{
			const int expected = 100000;
			var actual = 0;

			// we have ten background workers trying to consume messages from an internal buffer
			var producer = new BackgroundThreadProducer<BaseEvent> {MaxDegreeOfParallelism = 10};
			producer.Attach(x => { Interlocked.Increment(ref actual); });

			for (var i = 0; i < expected; i++)
				await producer.Produce(new BaseEvent {Id = i});

			await producer.Start();
			await producer.Stop();

			_console.WriteLine($"Delivered: {producer.Sent}");
			_console.WriteLine($"Uptime: {producer.Uptime}");
			_console.WriteLine($"Delivery rate: {producer.Rate} msgs / second");
		}

		[Fact]
		public async Task Can_cap_delivery_rate()
		{
			const int expected = 3000;
			var actual = 0;
			var producer = new BackgroundThreadProducer<BaseEvent> {MaxDegreeOfParallelism = 10};
			producer.Attach(x => { Interlocked.Increment(ref actual); });

			producer.RateLimitPolicy.Enabled = true;
			producer.RateLimitPolicy.Occurrences = 1000;
			producer.RateLimitPolicy.TimeUnit = TimeSpan.FromSeconds(1);

			for (var i = 0; i < expected; i++)
				await producer.Produce(new BaseEvent {Id = i});

			await producer.Start();
			await producer.Stop();

			Assert.Equal(expected, producer.Sent);
			Assert.True(Math.Abs(producer.Rate - producer.RateLimitPolicy.Occurrences) < expected * 1.25f);
			
			_console.WriteLine($"Delivered: {producer.Sent}");
			_console.WriteLine($"Uptime: {producer.Uptime}");
			_console.WriteLine($"Delivery rate: {producer.Rate} msgs / second");
		}

		[Fact]
		public async Task Can_manage_errors_with_custom_retry_policy()
		{
			const int expected = 1000;
			var actual = 0;
			var producer = new BackgroundThreadProducer<BaseEvent> { MaxDegreeOfParallelism = 10 };
			producer.AttachError(x => { /* compensating for not using an intermediary like Hub, which handles exceptions */});
			producer.AttachUndeliverable(x => Interlocked.Increment(ref actual));
			producer.Attach(new ThrowingHandler());
			producer.RetryPolicy.After(1, RetryDecision.Undeliverable);
			for (var i = 0; i < expected; i++)
				await producer.Produce(new BaseEvent { Id = i });
			await producer.Start();
			await producer.Stop();
		}

		[Fact]
		public async Task Can_requeue_with_exponential_backoff()
		{
			const int expected = 1000;
			var producer = new BackgroundThreadProducer<BaseEvent> { MaxDegreeOfParallelism = 10 };
			producer.AttachError(x => { /* compensating for not using an intermediary like Hub, which handles exceptions */});
			producer.Attach(new ThrowingHandler());
			for (var i = 0; i < expected; i++)
				await producer.Produce(new BaseEvent { Id = i });
			await producer.Start();
			await Task.Delay(1000);
			await producer.Stop();
		}
	}
}
