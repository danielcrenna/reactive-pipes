using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class StringEventHandler : IConsume<StringEvent>
    {
        public int Handled { get; private set; }
        
        public Task<bool> HandleAsync(StringEvent message)
        {
            Handled++;
            return Task.FromResult(true);
        }
    }
}