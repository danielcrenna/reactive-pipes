using System.Threading;

namespace reactive.pipes.examples
{
    public interface IExample
    {
        void Execute(AutoResetEvent block);
    }
}