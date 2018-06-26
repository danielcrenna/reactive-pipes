using System;
using System.Threading;
using System.Threading.Tasks;
using reactive.tests.Fakes;

namespace reactive.pipes.tests.Fakes
{
    public class LongRunningAsyncHandler : IConsume<IEvent>
    {
        public int Handled { get; private set; }

        public async Task<bool> HandleAsync(IEvent message)
        {
            await WaitAround();
            return true;
        }

        private async Task WaitAround()
        {
            await Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));
                Handled++;
            });
        }
    }
}