using System.Threading.Tasks;

namespace reactive.pipes.tests.Fakes
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