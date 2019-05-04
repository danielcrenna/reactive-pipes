// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace reactive.pipes.tests.Fakes
{
	public class BusyWaitHandler : IConsume<StringEvent>, IConsume<IntegerEvent>
	{
		private readonly int _secondsToWait;

		public BusyWaitHandler(int secondsToWait) => _secondsToWait = secondsToWait;

		public int HandledString { get; private set; }
		public int HandledInteger { get; private set; }

		public async Task<bool> HandleAsync(IntegerEvent message)
		{
			await MaybeWait();
			HandledInteger++;
			return true;
		}

		public async Task<bool> HandleAsync(StringEvent message)
		{
			await MaybeWait();
			HandledString++;
			return true;
		}

		private async Task MaybeWait()
		{
			if (_secondsToWait > 0)
				await Task.Delay(TimeSpan.FromSeconds(_secondsToWait));
		}
	}
}