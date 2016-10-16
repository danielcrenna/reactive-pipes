using System;

namespace reactive.pipes
{
    /// <summary>
    /// A message aggregator allows for topical subscriptions to some source.
    /// </summary>
    public interface IMessageAggregator
    {
        void Subscribe(object handler, Action<Exception> onError = null);
        void Subscribe<T>(Action<T> @handler, Action<Exception> onError = null);
        void Subscribe<T>(Action<T> @handler, Func<T, bool> topic, Action<Exception> onError = null);
        void Subscribe<T>(IConsume<T> consumer, Action<Exception> onError = null);
        void Subscribe<T>(IConsume<T> consumer, Func<T, bool> topic, Action<Exception> onError = null);
        void Unsubscribe<T>();
    }
}