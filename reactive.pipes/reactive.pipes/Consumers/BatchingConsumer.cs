using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using reactive.pipes.Extensions;

namespace reactive.pipes.Consumers
{
    /// <summary>
    /// Consumes events, and then emits them to a separate batched handler based on a batch window of number of events, or time interval
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BatchingConsumer<T> : IConsume<T>, IDisposable
    {
        private BlockingCollection<T> _collection;
        private CancellationTokenSource _cancel;
        private readonly IObservable<IList<T>> _observable;
        
        protected BatchingConsumer() : this(1000, TimeSpan.FromSeconds(1)) { }

        protected BatchingConsumer(int itemsPerBatch)
        {
            _observable = InitializeConsumer().Buffer(itemsPerBatch);
            Subscribe();
        }

        protected BatchingConsumer(int itemsPerBatch, TimeSpan orInterval)
        {
            _observable = InitializeConsumer().Buffer(orInterval, itemsPerBatch);
            Subscribe();
        }

        protected BatchingConsumer(TimeSpan interval)
        {
            _observable = InitializeConsumer().Buffer(interval);
            Subscribe();
        }

        private void Subscribe()
        {
            _observable.Subscribe(batch => Handle(batch), exception => { }, () => { }, _cancel.Token);
        }
        
        private IObservable<T> InitializeConsumer()
        {
            _cancel = new CancellationTokenSource();
            _collection = new BlockingCollection<T>();
            var observable = _collection.AsConsumingObservable(_cancel.Token);
            return observable;
        }
        
        public abstract bool Handle(IList<T> batch);

        public Task<bool> HandleAsync(T @event)
        {
            return Task.FromResult(_collection.TryAdd(@event));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_cancel == null) return;
            _cancel.Cancel();
            _cancel.Token.WaitHandle.WaitOne();
        }
    }
}