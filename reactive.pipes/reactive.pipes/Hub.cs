using System;
using System.Collections;
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
        private readonly ITypeResolver _typeResolver;

        private readonly Hashtable _byTypeDispatch = new Hashtable();
        private readonly ConcurrentDictionary<Type, WeakReference> _subscriptions = new ConcurrentDictionary<Type, WeakReference>();
        private readonly ConcurrentDictionary<Type, CancellationTokenSource> _unsubscriptions = new ConcurrentDictionary<Type, CancellationTokenSource>();

        public Hub(ITypeResolver assemblyResolver = null)
        {
            _typeResolver = assemblyResolver ?? new AppDomainTypeResolver();
        }

        public async Task<bool> PublishAsync<T>(T @event)
        {
            return await Task.Run(()=> Publish(@event));
        }

        public async Task<bool> PublishAsync(object @event)
        {
            return await Task.Run(() => Publish(@event));
        }

        public bool Publish(object @event)
        {
            Type type = @event.GetType();
            MethodInfo byTypeDispatch = _byTypeDispatch[type] as MethodInfo;
            if (byTypeDispatch == null)
            {
                const BindingFlags binding = BindingFlags.NonPublic | BindingFlags.Instance;
                MethodInfo method = typeof(Hub).GetMethod(nameof(PublishTyped), binding);
                byTypeDispatch = method.MakeGenericMethod(type);
                _byTypeDispatch[type] = byTypeDispatch;
            }
            return (bool) byTypeDispatch.Invoke(this, new[] {@event});
        }

        public bool Publish<T>(T @event)
        {
            return PublishTyped(@event);
        }

        private bool PublishTyped<T>(T @event)
        {
            WeakReference subscription;
            var subscriptionType = typeof(T);
            if (_subscriptions.TryGetValue(subscriptionType, out subscription))
            {
                WithEvent(@event, subscription);
                return true;
            }
            return false;
        }

        private static void WithEvent<T>(T @event, WeakReference subscription)
        {
            ISubject<T> subject = Box<T>(subscription);

            subject.OnNext(@event);
        }

        /// <summary> Subscribes a manifold handler. This is required if a handler acts as a consumer for more than one event type. </summary>
        public void Subscribe(object handler)
        {
            Type type = handler.GetType();
            Type[] interfaces = type.GetInterfaces();
            IEnumerable<Type> consumers = interfaces.Where(i => typeof(IConsume<>).IsAssignableFrom(i.GetGenericTypeDefinition()));

            const BindingFlags binding = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo subscribeByInterface = typeof(Hub).GetMethod(nameof(SubscribeByInterface), binding);
            MethodInfo subscribeSiblings = typeof(Hub).GetMethod(nameof(SubscribeSiblings), binding);

            // void Subscribe<T>(IConsume<T> consumer)
            foreach (var consumer in consumers)
            {
                // SubscribeByInterface(consumer);
                Type handlerType = consumer.GetGenericArguments()[0];
                subscribeByInterface.MakeGenericMethod(handlerType).Invoke(this, new[] { handler });

                // SubscribeSiblings<T>(typeof(T));
                subscribeSiblings.Invoke(this, new object[] { handlerType });
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            // Closest match:
            SubscribeByDelegate(handler);

            SubscribeSiblings(typeof(T));
        }

        public void Subscribe<T>(Action<T> handler, Func<T, bool> topic)
        {
            SubscribeByDelegateAndTopic(handler, topic);

            SubscribeSiblings(typeof(T));
        }

        public void Subscribe<T>(IConsume<T> consumer)
        {
            SubscribeByInterface(consumer);

            SubscribeSiblings(typeof(T));
        }

        public void Subscribe<T>(IConsume<T> consumer, Func<T, bool> topic)
        {
            SubscribeByInterfaceAndTopic(consumer, topic);

            SubscribeSiblings(typeof(T));
        }
        
        public void Unsubscribe<T>()
        {
            WeakReference reference;
            _subscriptions.TryRemove(typeof (T), out reference);

            CancellationTokenSource cancel;
            if(_unsubscriptions.TryGetValue(typeof(T), out cancel))
                cancel.Cancel();
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
            var subscriptionType = typeof(T);
            var unsubscription = _unsubscriptions.GetOrAdd(subscriptionType, t => new CancellationTokenSource());
            observable.Subscribe(@event => consumer.HandleAsync(@event), exception => { }, () => { }, unsubscription.Token);
        }

        private void SubscribeByInterfaceAndTopic<T>(IConsume<T> consumer, Func<T, bool> topic)
        {
            var subscription = GetSubscriptionSubject<T>();
            var observable = Box<T>(subscription).Where(topic).AsObservable();
            observable.Subscribe(@event => consumer.HandleAsync(@event), exception => { }, () => { });
        }

        private object GetSubscriptionSubject<T>()
        {
            return _subscriptions.GetOrAdd(typeof(T), t => new WeakReference(new Subject<T>()));
        }

        private static ISubject<T> Box<T>(object subscription)
        {
            object reference = ((WeakReference) subscription).Target;
            ISubject<T> subject = (ISubject<T>)reference;
            return subject;
        }

        private void SubscribeSiblings(Type baseType)
        {
            IEnumerable<Type> descendants = _typeResolver.GetDescendants(baseType);

           foreach (Type superType in descendants)
            {
                // This builds a forwarder for future subscriptions to supertypes, i.e.:
                // If we ever get an InheritedEvent, we want to call any subscribers to
                // BaseEvent and pass that event along
                SubscribeImplicit(superType, baseType);
            }
        }

        private void SubscribeImplicit(Type superType, Type baseType)
        {
            TypeInfo hubTypeInfo = typeof(Hub).GetTypeInfo();

            // WithEvent<BaseEvent>(event, subscription);
            MethodInfo withEvent = hubTypeInfo.GetMethod(nameof(WithEvent), BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(baseType);
            
            // Action<InheritedEvent>(e => WithEvent(e, subscription));
            var actionType = typeof(Action<>).MakeGenericType(superType);
            var action = new Action<object>(@event =>
            {
                // we are only allowed to forward if the parent has the same type, because
                // otherwise we will likely send the same event to a handler multiple times
                if (superType == @event.GetType())
                {
                    // forward the event on to downstream subscriptions, if we have them
                    WeakReference subscription;
                    if (_subscriptions.TryGetValue(baseType, out subscription))
                        withEvent.Invoke(this, new[] { @event, subscription });
                }
            });

            // SubscribeByDelegate(withEvent);
            const BindingFlags subscribeBinding = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo subscribeByDelegate = hubTypeInfo.GetMethod(nameof(SubscribeByDelegate), subscribeBinding).MakeGenericMethod(superType);
            
            MethodInfo mi = action.GetMethodInfo();
            Delegate closure = mi.CreateDelegate(actionType, action.Target);
            subscribeByDelegate.Invoke(this, new object[] { closure });
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
                ((IDisposable)subscription.Value.Target).Dispose();
        }
    }
}