﻿// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace reactive.pipes.tests.Fakes
{
	public class FailingHandler : IConsume<IEvent>
	{
		public int Handled { get; private set; }

		public Task<bool> HandleAsync(IEvent message)
		{
			Handled++;
			return Task.FromResult(false);
		}
	}
}