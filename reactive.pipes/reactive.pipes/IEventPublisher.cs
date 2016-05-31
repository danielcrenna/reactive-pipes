using System;
using System.Threading.Tasks;

namespace reactive.pipes
{
    /// <summary>
    /// An event publisher allows eventing to some consumers.
    /// </summary>
    public interface IEventPublisher : IDisposable
    {
        Task<bool> PublishAsync<T>(T @event);
        bool Publish<T>(T @event);
    }
}