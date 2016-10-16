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
    /// Consumes events, and then emits them to a separate batched handler based on a batch window of number of events, or time interval.
    /// 
    /// Produces events that fail to process in a batch, in order to gracefully handle batch failures when our original production source may be long gone.
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BatchingConsumer<T> : IConsume<T>, IProduce<T>, IDisposable
    {
        private BlockingCollection<T> _collection;
        private CancellationTokenSource _cancel;
        private readonly IObservable<IList<T>> _observable;
        private IConsume<T> _undeliverableConsumer;

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
            _observable.Subscribe(HandleBatchInternal, exception => { }, () => { }, _cancel.Token);
        }

        private async void HandleBatchInternal(IList<T> batch)
        {
            if (!Handle(batch))
            {
                foreach (var item in batch)
                    await HandleUndeliverable(item);
            }
        }

        private IObservable<T> InitializeConsumer()
        {
            _cancel = new CancellationTokenSource();
            _collection = new BlockingCollection<T>();
            _undeliverableConsumer = new ActionConsumer<T>(@event => { });

            return _collection.AsConsumingObservable(_cancel.Token);
        }
        
        public abstract bool Handle(IList<T> batch);

        public virtual Task<bool> HandleAsync(T message)
        {
            Task<bool> added = Task.FromResult(_collection.TryAdd(message));

            return added;
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

        public void Attach(IConsume<T> consumer)
        {
            _undeliverableConsumer = consumer;
        }

        public void Attach(Action<T> consumer)
        {
            _undeliverableConsumer = new ActionConsumer<T>(consumer);
        }

        private async Task HandleUndeliverable(T @event)
        {
            await _undeliverableConsumer.HandleAsync(@event);
        }
    }
}