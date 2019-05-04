using System;
using BenchmarkDotNet.Running;

namespace reactive.pipes.benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ConsumerBenchmarks>();
        }
    }
}
