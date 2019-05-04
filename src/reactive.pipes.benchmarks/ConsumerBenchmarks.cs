using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace reactive.pipes.benchmarks
{
    [CoreJob]
    [MarkdownExporter]
    [MemoryDiagnoser]
    public class ConsumerBenchmarks
    {
        [Params(1_000_000)] public int Trials;

        private Hub _default;
        private Hub _unsafe;
        private Hub _manifold;

        [GlobalSetup]
        public void GlobalSetup()
        {
	        _default = new Hub();
	        _default.Subscribe(new DoNothingHandler());

	        _manifold = new Hub();
	        _manifold.Subscribe(new ManifoldMessage());

            _unsafe = new Hub {DispatchConcurrencyMode = DispatchConcurrencyMode.Unsafe};
            _unsafe.Subscribe(new BusyWaitMessage());
        }

        [Benchmark]
        public void ReactivePipes_single_subscription_sequential()
        {
            for (var i = 0; i < Trials; i++)
            {
	            _default.Publish(new BaseMessage());
            }
        }

        [Benchmark]
        public void ReactivePipes_single_subscription_async()
        {
	        for (var i = 0; i < Trials; i++)
	        {
		        _default.PublishAsync(new BaseMessage());
	        }
        }

        [Benchmark]
        public void ReactivePipes_single_subscription_unsafe_wait_async()
        {
	        for (var i = 0; i < Trials; i++)
            {
	            _unsafe.PublishAsync(new BaseMessage());
            }
        }

        [Benchmark]
        public void ReactivePipes_manifold_subscription_sequential()
        {
            for (var i = 0; i < Trials; i++)
            {
	            _manifold.Publish(new SubMessage());
            }
        }

        [Benchmark]
        public void ReactivePipes_manifold_subscription_async()
        {
	        for (var i = 0; i < Trials; i++)
	        {
		        _manifold.PublishAsync(new SubMessage());
	        }
        }

        public class BaseMessage { }

        public class SubMessage : BaseMessage { }

        public class DoNothingHandler : IConsume<BaseMessage>
        {
            public Task<bool> HandleAsync(BaseMessage message)
            {
                return Task.FromResult(true);
            }

            public bool Handle(BaseMessage message)
            {
                return true;
            }
        }

        public class BusyWaitMessage : IConsume<BaseMessage>
        {
            public Task<bool> HandleAsync(BaseMessage message)
            {
                return Task.FromResult(true);
            }

            public bool Handle(BaseMessage message)
            {
                Task.Delay(5000).Wait();
                return true;
            }
        }

        public class ManifoldMessage : IConsume<BaseMessage>, IConsume<SubMessage>
        {
            public Task<bool> HandleAsync(BaseMessage message)
            {
                return Task.FromResult(true);
            }

            public Task<bool> HandleAsync(SubMessage message)
            {
                return Task.FromResult(true);
            }

            public bool Handle(BaseMessage message)
            {
                return true;
            }

            public bool Handle(SubMessage message)
            {
                return true;
            }
        }
    }
}
