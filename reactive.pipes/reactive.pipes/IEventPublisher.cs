using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace reactive.pipes
{
    /// <summary>
    /// An event publisher allows eventing to some consumers.
    /// </summary>
    public interface IEventPublisher : IDisposable
    {
        Task<bool> PublishAsync(object @event);
        Task<bool> PublishAsync<T>(T @event);

        bool Publish(object @event);
        bool Publish<T>(T @event);
    }

    
}