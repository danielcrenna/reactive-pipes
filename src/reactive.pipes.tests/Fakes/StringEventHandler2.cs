using System.Threading.Tasks;
using reactive.tests.Fakes;

namespace reactive.pipes.tests.Fakes
{
	public class StringEventHandler2 : IConsume<StringEvent>
	{
		public int Handled { get; private set; }

		public Task<bool> HandleAsync(StringEvent message)
		{
			Handled++;
			return Task.FromResult(true);
		}
	}
}