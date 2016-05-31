using System.Threading.Tasks;
using reactive.pipes;

namespace reactive.tests
{
    public class StringEventHandler : IConsume<StringEvent>
    {
        public bool Handled { get; private set; }

        public bool Handle(StringEvent @event)
        {
            Handled = true;
            return true;
        }

        public async Task<bool> HandleAsync(StringEvent @event)
        {
            return await Task.Factory.StartNew(() => Handle(@event));
        }
    }
}