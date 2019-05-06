// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using reactive.pipes.tests.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace reactive.pipes.tests
{
	public class PerformanceTests
	{
		private readonly ITestOutputHelper _output;

		public PerformanceTests(ITestOutputHelper output) => _output = output;

		[Theory]
		[InlineData(1)]
		public async void Profile_message_handling_for_simple_handler(int seconds)
		{
			var handler = new PerformanceEventHandler();

			var hub = new Hub();
			hub.Subscribe(handler);

			var random = new Random();
			var cancel = new CancellationTokenSource();
			var token = cancel.Token;

			var sw = Stopwatch.StartNew();
			var blaster = Task.Run(() =>
			{
				while (!cancel.IsCancellationRequested) hub.PublishAsync(new StringEvent(random.Next().ToString()));
			}, token);

			await Task.Delay(TimeSpan.FromSeconds(seconds), token);

			cancel.Cancel();

			_output.WriteLine(
				$"Handled {handler.HandledString} messages in {sw.Elapsed.TotalSeconds} seconds. ({handler.HandledString / sw.Elapsed.TotalSeconds:F2} msg/sec)");
		}

		[Theory]
		[InlineData(10)]
		public async void Profile_busy_wait_over_single_subscription(int seconds)
		{
			var hub = new Hub();
			var handler = new BusyWaitHandler(5);

			hub.Subscribe(handler);

			var random = new Random();
			var cancel = new CancellationTokenSource();
			var token = cancel.Token;

			var sw = Stopwatch.StartNew();
			var blaster = Task.Run(() =>
			{
				while (!cancel.IsCancellationRequested) hub.PublishAsync(new StringEvent(random.Next().ToString()));
			}, token);

			await Task.Delay(TimeSpan.FromSeconds(seconds), token);

			cancel.Cancel();

			_output.WriteLine(
				$"Handled {handler.HandledString} messages in {sw.Elapsed.TotalSeconds} seconds. ({handler.HandledString / sw.Elapsed.TotalSeconds:F2} msg/sec)");
		}

		[Theory]
		[InlineData(10)]
		public async void Profile_busy_wait_over_single_unsafe(int seconds)
		{
			var hub = new Hub {DispatchConcurrencyMode = DispatchConcurrencyMode.Unsafe};
			var handler = new BusyWaitHandler(5);

			hub.Subscribe(handler);

			var random = new Random();
			var cancel = new CancellationTokenSource();
			var token = cancel.Token;

			var sw = Stopwatch.StartNew();
			var blaster = Task.Run(() =>
			{
				while (!cancel.IsCancellationRequested)
				{
					// remove the thread pool as a bottleneck
					var thread = new Thread(() => { hub.Publish(new StringEvent(random.Next().ToString())); });
					thread.Start();
				}
			}, token);

			await Task.Delay(TimeSpan.FromSeconds(seconds), token);

			cancel.Cancel();

			_output.WriteLine(
				$"Handled {handler.HandledString} messages in {sw.Elapsed.TotalSeconds} seconds. ({handler.HandledString / sw.Elapsed.TotalSeconds:F2} msg/sec)");
		}

		[Theory]
		[InlineData(10)]
		public async void Profile_busy_wait_over_multiple_subscriptions_with_topic_splitting(int seconds)
		{
			var hub = new Hub();
			var handler = new BusyWaitHandler(5);

			var partitionKey1 = Guid.NewGuid();
			var partitionKey2 = Guid.NewGuid();

			hub.SubscriptionKeyMode = SubscriptionKeyMode.Topical; 
			hub.Subscribe((IConsume<StringEvent>) handler, e => e.PartitionKey == partitionKey1);
			hub.Subscribe((IConsume<StringEvent>) handler, e => e.PartitionKey == partitionKey2);

			var random = new Random();
			var cancel = new CancellationTokenSource();
			var token = cancel.Token;

			var sw = Stopwatch.StartNew();
			var distribution = 0;
			var blaster = Task.Run(() =>
			{
				while (!cancel.IsCancellationRequested)
				{
					var message = new StringEvent(random.Next().ToString())
					{
						PartitionKey = distribution++ % 2 == 1 ? partitionKey2 : partitionKey1
					};
					hub.PublishAsync(message);
				}
			}, token);

			await Task.Delay(TimeSpan.FromSeconds(seconds), token);

			cancel.Cancel();

			_output.WriteLine(
				$"Handled {handler.HandledString} messages in {sw.Elapsed.TotalSeconds} seconds. ({handler.HandledString / sw.Elapsed.TotalSeconds:F2} msg/sec)");
		}
	}
}