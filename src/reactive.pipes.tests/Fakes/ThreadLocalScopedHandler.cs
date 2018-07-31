using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace reactive.pipes.tests.Fakes
{
	public class ThreadLocalScopedHandler : IConsumeScoped<BaseEvent>
	{
		readonly ThreadLocal<List<string>> _cache;

		public List<string> Lines { get; } = new List<string>();

		public ThreadLocalScopedHandler(ThreadLocal<List<string>> cache)
		{
			_cache = cache;
		}

		public bool Before()
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

		public bool After(bool result)
		{
			if (result)
				_cache.Value.Add("After");
			return result;
		}
	}
}