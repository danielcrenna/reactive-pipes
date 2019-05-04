// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Reactive.Linq;
using System.Threading;
using reactive.pipes.Consumers;
using reactive.pipes.Producers;
using reactive.pipes.Serializers;

namespace reactive.pipes.examples
{
	/// <summary>
	///     The most basic scenario is attaching a consumer to a producer.  In this case, the producer is sending a sequence of
	///     numbers.
	///     The consumer is sending those numbers to the console.
	/// </summary>
	public class HelloWorld : IExample
	{
		public void Execute(AutoResetEvent block)
		{
			var producer = new ObservingProducer<int>();
			var consumer = new ActionConsumer<int>(i => Console.WriteLine(i));

			producer.Attach(consumer);
			producer.Produces(Observable.Range(1, 10000), onCompleted: () => block.Set());
			producer.Start();
		}
	}

	/// <summary>
	///     This is the basic scenario, using the fluent API. It helps with writing code in a task-oriented way.
	/// </summary>
	public class HelloWorldChain : IExample
	{
		public void Execute(AutoResetEvent block)
		{
			var producer = new ObservingProducer<int>();
			producer.Produces(Observable.Range(1, 10000), onCompleted: () => block.Set());
			producer.Attach(Console.WriteLine);
			producer.Start();
		}
	}

	/// <summary>
	///     Frequently you want to process batches of events as they come in rather than one at a time.
	///     This example demonstrates receiving a range of integers, but processing them in 10 batches of 1000.
	/// </summary>
	public class Batching : IExample
	{
		public void Execute(AutoResetEvent block)
		{
			var producer = new ObservingProducer<int>();
			producer.Produces(Observable.Range(1, 10000), onCompleted: () => block.Set());
			producer.Attach(new ActionBatchingConsumer<int>(i => Console.WriteLine(i.Count), 1000));
			producer.Start();
		}
	}

	/// <summary>
	///     When you want your infrastructure to send events off-network, you need to serialize those events first.
	///     Often this responsibility is left to the producer itself, but you can use the built-in transport pipe
	///     to elevate the serialization responsibility and work with streams instead. Here, integers are
	///     serialized on one end, deserialized on the other, and neither the originating producer or final
	///     consumer knows about it.
	/// </summary>
	public class Transport : IExample
	{
		public void Execute(AutoResetEvent block)
		{
			var producer = new ObservingProducer<int>();
			var consumer = new ActionConsumer<int>(i => Console.WriteLine(i));

			var serializer = new BinarySerializer();
			var outbound =
				new ProtocolProducer<int>(
					serializer); // This is a producer of a data stream that consumes T events (serializer)
			var inbound =
				new ProtocolConsumer<int>(
					serializer); // This is a consumer of a data stream that produces T events (deserializer)
			outbound.Attach(
				inbound); // Typically you'd put an enqueing consumer here to shuttle serialized events off-network
			inbound.Attach(consumer);

			producer.Attach(outbound);
			producer.Produces(Observable.Range(1, 10000), onCompleted: () => block.Set());
			producer.Start();
		}
	}

	/// <summary>
	///     In this example, events are pushed to disk by a consumer while they are being picked up by a producer on a
	///     background thread.
	///     If files matching the criteria already exist when the producer is started, they are picked up as well.
	/// </summary>
	public class FileStore : IExample
	{
		public void Execute(AutoResetEvent block)
		{
			const int filesToCreate = 100;
			var filesProcessed = 0;

			// This setup will emit files on another thread
			var producesInts = new ObservingProducer<int>();
			producesInts.Produces(Observable.Range(1, filesToCreate)).Attach(new FileConsumer<int>());
			producesInts.Start();

			// This setup will output the contents of loaded files to the console
			var fileProducer = new FileProducer<int>();
			var logger = new ActionConsumer<int>(i =>
			{
				filesProcessed++;
				Console.WriteLine(filesProcessed);
				if (filesProcessed >= filesToCreate) block.Set();
			});
			fileProducer.Attach(logger);
			fileProducer.Start();
		}
	}
}