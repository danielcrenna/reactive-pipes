using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class ManifoldEventHandler : IConsume<StringEvent>, IConsume<IntegerEvent>
    {
        public int HandledString { get; private set; }
        public int HandledInteger { get; private set; }

        public Task<bool> HandleAsync(StringEvent message)
        {
            HandledString++;
            return Task.FromResult(true);
        }

        public Task<bool> HandleAsync(IntegerEvent message)
        {
            HandledInteger++;
            return Task.FromResult(true);
        }
    }
}