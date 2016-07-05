using System;
using System.Threading.Tasks;

namespace reactive.pipes
{
    /// <summary>
    /// An event publisher allows eventing to some consumers.
    /// </summary>
    public interface IEventPublisher : IDisposable
    {
        Task<bool> PublishAsync(object @event);
        bool Publish(object @event);
    }
}