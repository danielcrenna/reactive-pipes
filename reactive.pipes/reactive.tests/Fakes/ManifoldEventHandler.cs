using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class ManifoldEventHandler : IConsume<StringEvent>, IConsume<IntegerEvent>
    {
        public bool HandledString { get; private set; }
        public bool HandledInteger { get; private set; }

        public Task<bool> HandleAsync(StringEvent @event)
        {
            HandledString = true;
            return Task.FromResult(true);
        }

        public Task<bool> HandleAsync(IntegerEvent @event)
        {
            HandledInteger = true;
            return Task.FromResult(true);
        }
    }
}