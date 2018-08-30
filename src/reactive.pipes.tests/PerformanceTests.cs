using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using reactive.pipes.tests.Fakes;
using Xunit;
using Xunit.Abstractions;

namespace reactive.pipes.tests
{
    public class PerformanceTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Theory, InlineData(1)]
        public async void Profile_message_handling_for_simple_handler(int seconds)
        {
            PerformanceEventHandler handler = new PerformanceEventHandler();

            var hub = new Hub();
            hub.Subscribe(handler);

            var random = new Random();
            var canceller = new CancellationTokenSource();
            var token = canceller.Token;

            var sw = Stopwatch.StartNew();
            var blaster = Task.Run(() =>
            {
                while(!canceller.IsCancellationRequested)
                {
                    hub.PublishAsync(new StringEvent(random.Next().ToString()));
                }
            }, token);
            
            await Task.Delay(TimeSpan.FromSeconds(seconds), token);

            canceller.Cancel();

            _output.WriteLine($"Handled {handler.HandledString} messages in {sw.Elapsed.TotalSeconds} seconds. ({handler.HandledString / sw.Elapsed.TotalSeconds:F2} msg/sec)");
        }

    }
}
