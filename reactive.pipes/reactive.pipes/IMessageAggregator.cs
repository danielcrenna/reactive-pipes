using System;

namespace reactive.pipes
{
    /// <summary>
    /// A message aggregator allows for topical subscriptions to some source.
    /// </summary>
    public interface IMessageAggregator
    {
        void Subscribe<T>(Action<T> @handler);
        void Subscribe<T>(Action<T> @handler, Func<T, bool> topic);
        void Subscribe<T>(IConsume<T> consumer);
        void Subscribe<T>(IConsume<T> consumer, Func<T, bool> topic);
        void Unsubscribe<T>();
    }
}