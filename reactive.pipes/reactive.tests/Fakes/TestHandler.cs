using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class TestHandler : IConsume<IEvent>
    {
        public bool Handled { get; private set; }

        public Task<bool> HandleAsync(IEvent @event)
        {
            Handled = true;
            return Task.FromResult(Handled);
        }
    }
}