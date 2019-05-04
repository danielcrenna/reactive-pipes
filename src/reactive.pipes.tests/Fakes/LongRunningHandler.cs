// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace reactive.pipes.tests.Fakes
{
	public class LongRunningAsyncHandler : IConsume<IEvent>
	{
		public int Handled { get; private set; }

		public async Task<bool> HandleAsync(IEvent message)
		{
			await WaitAround();
			return true;
		}

		private async Task WaitAround()
		{
			await Task.Run(() =>
			{
				Thread.Sleep(TimeSpan.FromSeconds(2));
				Handled++;
			});
		}
	}
}