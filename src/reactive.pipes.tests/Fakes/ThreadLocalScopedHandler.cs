// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace reactive.pipes.tests.Fakes
{
	public class ThreadLocalScopedHandler : IConsumeScoped<BaseEvent>
	{
		private readonly ThreadLocal<List<string>> _cache;

		public ThreadLocalScopedHandler(ThreadLocal<List<string>> cache) => _cache = cache;

		public List<string> Lines { get; } = new List<string>();

		public bool Before(BaseEvent message)
		{
			_cache.Value.Add("Before");
			return true;
		}

		public Task<bool> HandleAsync(BaseEvent message)
		{
			foreach (var line in _cache.Value)
				Lines.Add(line);
			return Task.FromResult(true);
		}

		public bool After(BaseEvent message, bool result)
		{
			if (result)
				_cache.Value.Add("After");
			return result;
		}
	}
}