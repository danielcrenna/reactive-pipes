using System;
using System.Threading;
using System.Threading.Tasks;

namespace reactive.pipes.examples
{
    class Program
    {
        static void Main(string[] args)
        {
            var block = new AutoResetEvent(false);

            Task.Factory.StartNew(() =>
                new HelloWorld().Execute(block)
            //new HelloWorldChain().Execute(block)
            //new Batching().Execute(block)
            //new Transport().Execute(block)
            //new FileStore().Execute(block)
            );

            block.WaitOne();
            Console.ReadKey();
        }
    }
}
