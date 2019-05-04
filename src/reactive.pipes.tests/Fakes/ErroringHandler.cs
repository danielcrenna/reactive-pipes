// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace reactive.pipes.tests.Fakes
{
	public class ErroringHandler : IConsume<ErrorEvent>
	{
		public Task<bool> HandleAsync(ErrorEvent message)
		{
			if (message.Error)
				throw new Exception("The message made me do it!");
			return Task.FromResult(true);
		}
	}
}