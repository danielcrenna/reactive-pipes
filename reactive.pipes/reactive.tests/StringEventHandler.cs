using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests
{
    public class StringEventHandler : IConsume<StringEvent>
    {
        public bool Handled { get; private set; }
        
        public async Task<bool> HandleAsync(StringEvent @event)
        {
            return await Task.Run(() => Handled = true);
        }
    }
}