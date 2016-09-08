using System;
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

    public class ErroringHandler : IConsume<ErrorEvent>
    {
        public Task<bool> HandleAsync(ErrorEvent message)
        {
            throw new Exception("The message made me do it!");
        }
    }
}