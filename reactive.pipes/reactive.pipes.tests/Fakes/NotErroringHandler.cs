using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class NotErroringHandler : IConsume<ErrorEvent>
    {
        public Task<bool> HandleAsync(ErrorEvent message)
        {
            return Task.FromResult(true);
        }
    }
}