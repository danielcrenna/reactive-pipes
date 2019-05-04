// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using reactive.pipes.Consumers;

namespace reactive.pipes
{
	public static class ProducerExtensions
	{
		public static void Attach<T>(this IProduce<T> producer, Action<T> consumer)
		{
			producer.Attach(new ActionConsumer<T>(consumer));
		}
	}
}