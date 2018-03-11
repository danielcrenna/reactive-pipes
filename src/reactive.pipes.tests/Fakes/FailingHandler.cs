using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class FailingHandler : IConsume<IEvent>
    {
        public int Handled { get; private set; }

        public Task<bool> HandleAsync(IEvent message)
        {
            Handled++;
            return Task.FromResult(false);
        }
    }
}