using System.Threading.Tasks;
using reactive.tests.Fakes;

namespace reactive.pipes.tests.Fakes
{
    public class NotErroringHandler : IConsume<ErrorEvent>
    {
        public Task<bool> HandleAsync(ErrorEvent message)
        {
            return Task.FromResult(true);
        }
    }
}