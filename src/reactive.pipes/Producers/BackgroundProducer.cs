using System;
using System.Threading.Tasks;

namespace reactive.pipes.Producers
{
    /// <summary>
    /// A base implementation for a producer that uses the default background producer as its worker thread.
    /// <remarks>
    /// See implementers for reference implementation; basically you subscribe a production to the Background directly in the constructor
    /// </remarks>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BackgroundProducer<T> : IProduce<T>, IDisposable
    {
        protected BackgroundThreadProducer<T> Background { get; private set; }

        protected BackgroundProducer()
        {
            Background = new BackgroundThreadProducer<T>();
        }

        public virtual void Attach(IConsume<T> consumer)
        {
            Background.Attach(consumer);
        }

        public virtual Task Start(bool immediate = false)
        {
            return Background.Start(immediate);
        }

        public virtual Task Stop(bool immediate = false)
        {
            return Background.Stop(immediate);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            Background?.Dispose();
            Background = null;
        }
    }
}