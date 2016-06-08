using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests.Fakes
{
    public class StringEventHandler : IConsume<StringEvent>
    {
        public bool Handled { get; private set; }
        
        public Task<bool> HandleAsync(StringEvent @event)
        {
            Handled = true;
            return Task.FromResult(true);
        }
    }
}