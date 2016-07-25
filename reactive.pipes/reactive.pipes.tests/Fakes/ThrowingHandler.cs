using reactive.pipes;
using System;
using System.Threading.Tasks;

namespace reactive.tests.Fakes
{
    public class ThrowingHandler : IConsume<IEvent>
    {
        public int Handled { get; private set; }

        public Task<bool> HandleAsync(IEvent @event)
        {
            Handled++;
            throw new Exception();
        }
    }
}
