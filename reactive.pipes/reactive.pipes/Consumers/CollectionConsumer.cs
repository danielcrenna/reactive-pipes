using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace reactive.pipes.Consumers
{
    /// <summary>
    /// A consumer that forwards all handled events to an in-memory blocking collection
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CollectionConsumer<T> : IConsume<T>, IEnumerable<T>, IDisposable
    {
        private BlockingCollection<T> _collection;

        public CollectionConsumer()
        {
            _collection = new BlockingCollection<T>();
        }

        public CollectionConsumer(IProducerConsumerCollection<T> collection)
        {
            _collection = new BlockingCollection<T>(collection);
        }

        public CollectionConsumer(BlockingCollection<T> collection)
        {
            _collection = collection;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _collection.GetConsumingEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public int Count => _collection.Count;

        public Task<bool> HandleAsync(T @event)
        {
            return Task.FromResult(_collection.TryAdd(@event));
        }

        public virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (_collection == null) return;
            _collection.Dispose();
            _collection = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}