using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class ManifoldEventHandler : IConsume<StringEvent>, IConsume<IntegerEvent>
    {
        public int HandledString { get; private set; }
        public int HandledInteger { get; private set; }

        public Task<bool> HandleAsync(StringEvent @event)
        {
            HandledString++;
            return Task.FromResult(true);
        }

        public Task<bool> HandleAsync(IntegerEvent @event)
        {
            HandledInteger++;
            return Task.FromResult(true);
        }
    }
}