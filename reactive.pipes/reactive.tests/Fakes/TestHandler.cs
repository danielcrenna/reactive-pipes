using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class TestHandler : IConsume<IEvent>
    {
        public int Handled { get; private set; }

        public Task<bool> HandleAsync(IEvent @event)
        {
            Handled++;
            return Task.FromResult(true);
        }
    }
}