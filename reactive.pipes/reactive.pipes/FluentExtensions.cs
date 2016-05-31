using System;
using reactive.pipes.Consumers;

namespace reactive.pipes
{
    public static class FluentExtensions
    {
        public static IProduce<T> Consumes<T>(this IProduce<T> producer, IConsume<T> consumer)
        {
            producer.Attach(consumer);
            return producer;
        }

        public static IProduce<T> Consumes<T>(this IProduce<T> producer, Action<T> consumer)
        {
            producer.Attach(new ActionConsumer<T>(consumer));
            return producer;
        }
    }
}