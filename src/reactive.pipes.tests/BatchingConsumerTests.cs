// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using reactive.pipes.Consumers;
using reactive.pipes.tests.Fakes;
using Xunit;

namespace reactive.pipes.tests
{
	public class BatchingConsumerTests
	{
		[Theory]
		[InlineData(1)]
		[InlineData(10)]
		[InlineData(100)]
		[InlineData(1000)]
		[InlineData(10000)]
		public async void Handles_items_per_batch(int itemsPerBatch)
		{
			var handled = 0;

			var consumer = new ActionBatchingConsumer<StringEvent>(list => { handled += list.Count; }, itemsPerBatch,
				TimeSpan.FromHours(1));

			for (var i = 0; i < itemsPerBatch; i++)
				await consumer.HandleAsync(new StringEvent());

			await Task.Delay(TimeSpan.FromMilliseconds(10));

			Assert.Equal(itemsPerBatch, handled);
		}

		[Fact]
		public async void Handles_interval()
		{
			var handled = 0;

			var consumer = new ActionBatchingConsumer<StringEvent>(list => { handled += list.Count; }, 100,
				TimeSpan.FromMilliseconds(10));

			for (var i = 0; i < 100; i++)
				await consumer.HandleAsync(new StringEvent());

			await Task.Delay(TimeSpan.FromMilliseconds(10));

			Assert.Equal(100, handled);
		}

		[Fact]
		public async void Handles_production_on_partial_batch_non_delivery()
		{
			var handled = 0;
			var error = false;

			var consumer = new ActionBatchingConsumer<StringEvent>(list =>
			{
				if (!error)
				{
					handled += list.Count;
					error = true;
				}
				else
				{
					error = false;
					throw new Exception();
				}
			}, 100, TimeSpan.FromMilliseconds(10));

			var undelivered = 0;

			consumer.Attach(e => undelivered++);

			for (var i = 0; i < 150; i++)
				await consumer.HandleAsync(new StringEvent());

			await Task.Delay(TimeSpan.FromSeconds(1));

			Assert.Equal(100, handled);

			Assert.Equal(50, undelivered);
		}
	}
}