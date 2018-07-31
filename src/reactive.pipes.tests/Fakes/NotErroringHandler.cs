using System.Threading.Tasks;

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