using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace reactive.pipes
{
    /// <summary>
    /// Allows subscribing handlers to centralized publishing events, and allows subscription by topic.
    /// 
    /// Basically, it is both an aggregator and a publisher, and is typically used to provide pub-sub services in-process.
    /// </summary>
    public class Hub : IEventAggregator, IEventPublisher
    {
        private readonly ConcurrentDictionary<Type, WeakReference> _subscriptions = new ConcurrentDictionary<Type, WeakReference>();
        private readonly ConcurrentDictionary<Type, CancellationTokenSource> _unsubscriptions = new ConcurrentDictionary<Type, CancellationTokenSource>();

        public async Task<bool> PublishAsync<T>(T @event)
        {
            return await Task.Run(()=> Publish(@event));
        }

        public bool Publish<T>(T @event)
        {
            WeakReference subscription;
            if (_subscriptions.TryGetValue(typeof (T), out subscription))
            {
                WithEvent(@event, subscription);
                return true;
            }
            return false;
        }

        private static void WithEvent<T>(T @event, WeakReference subscription)
        {
            Box<T>(subscription).OnNext(@event);
        }

        /// <summary> Subscribes a manifold handler. This is required if a handler acts as a consumer for more than one event type. </summary>
        public void Subscribe(object handler)
        {
            Type type = handler.GetType();
            Type[] interfaces = type.GetInterfaces();
            IEnumerable<Type> consumers = interfaces.Where(i => typeof(IConsume<>).IsAssignableFrom(i.GetGenericTypeDefinition()));

            const BindingFlags binding = BindingFlags.Instance | BindingFlags.NonPublic;

            foreach (var consumer in consumers)
            {
                Type handlerType = consumer.GetGenericArguments()[0];
                MethodInfo method = typeof(Hub).GetMethod("SubscribeByInterface", binding);
                MethodInfo generic = method.MakeGenericMethod(handlerType);
                generic.Invoke(this, new[] { handler });
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            // Closest match:
            SubscribeByDelegate(handler);

            // Sibling tree (a delegate that fans out to all base event handlers that are subscribed):
            Type type = typeof(T);
            while (type != null && type.BaseType != typeof(object))
            {
                type = type.BaseType; // <-- go to next in base class hierarchy

                Type baseType = type;

                const BindingFlags binding = BindingFlags.Static | BindingFlags.NonPublic;
                MethodInfo method = typeof(Hub).GetMethod("WithEvent", binding);
                MethodInfo generic = method.MakeGenericMethod(baseType);
                
                Action<T> fanOut = @event =>
                {
                    // A new handler for the top-level event type, that fans out dynamically to current sub-type subscriptions
                    WeakReference subscription;
                    if (baseType != null && _subscriptions.TryGetValue(baseType, out subscription))
                    {
                        generic.Invoke(this, new object[] { @event, subscription });
                    }
                };

                SubscribeByDelegate(fanOut);
            }
        }

        public void Subscribe<T>(Action<T> handler, Func<T, bool> topic)
        {
            SubscribeByDelegateAndTopic(handler, topic);
        }

        public void Subscribe<T>(IConsume<T> consumer)
        {
            SubscribeByInterface(consumer);
        }

        public void Subscribe<T>(IConsume<T> consumer, Func<T, bool> topic)
        {
            SubscribeByInterfaceAndTopic(consumer, topic);
        }
        
        public void Unsubscribe<T>()
        {
            WeakReference reference;
            _subscriptions.TryRemove(typeof (T), out reference);

            CancellationTokenSource cancel;
            if(_unsubscriptions.TryGetValue(typeof(T), out cancel))
            {
                cancel.Cancel();
            }
        }

        private object GetSubscriptionSubject<T>()
        {
            return _subscriptions.GetOrAdd(typeof(T), t => new WeakReference(new Subject<T>()));
        }

        private void SubscribeByDelegate<T>(Action<T> handler)
        {
            var subscription = GetSubscriptionSubject<T>();
            var observable = Box<T>(subscription).AsObservable();
            observable.Subscribe(handler);
        }

        private void SubscribeByDelegateAndTopic<T>(Action<T> handler, Func<T, bool> topic)
        {
            var subscription = GetSubscriptionSubject<T>();
            var observable = Box<T>(subscription).Where(topic).AsObservable();
            observable.Subscribe(handler);
        }

        private void SubscribeByInterface<T>(IConsume<T> consumer)
        {
            var subscription = GetSubscriptionSubject<T>();
            var observable = Box<T>(subscription).AsObservable();
            var unsubscription = _unsubscriptions.GetOrAdd(typeof(T), t => new CancellationTokenSource());
            observable.Subscribe(@event => consumer.HandleAsync(@event), exception => { }, () => { }, unsubscription.Token);
        }

        private void SubscribeByInterfaceAndTopic<T>(IConsume<T> consumer, Func<T, bool> topic)
        {
            var subscription = GetSubscriptionSubject<T>();
            var observable = Box<T>(subscription).Where(topic).AsObservable();
            observable.Subscribe(@event => consumer.HandleAsync(@event), exception => { }, () => { });
        }

        private static ISubject<T> Box<T>(object subscription)
        {
            var reference = ((WeakReference) subscription).Target;
            return (ISubject<T>)reference;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if(!disposing || _subscriptions == null || _subscriptions.Count == 0)
                return;

            foreach (var subscription in _subscriptions.Where(subscription => subscription.Value.Target is IDisposable))
            {
                ((IDisposable)subscription.Value.Target).Dispose();
            }
        }
    }
}