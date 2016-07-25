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
    public class Hub : IMessageAggregator, IMessagePublisher
    {
        private readonly ITypeResolver _typeResolver;

        private readonly Hashtable _byTypeDispatch = new Hashtable();
        private readonly ConcurrentDictionary<Type, IDisposable> _subscriptions = new ConcurrentDictionary<Type, IDisposable>();
        private readonly ConcurrentDictionary<Type, CancellationTokenSource> _unsubscriptions = new ConcurrentDictionary<Type, CancellationTokenSource>();
        private readonly ConcurrentDictionary<Type, bool> _results = new ConcurrentDictionary<Type, bool>();

        public Hub(ITypeResolver assemblyResolver = null)
        {
            _typeResolver = assemblyResolver ?? new DefaultTypeResolver();
        }

        public async Task<bool> PublishAsync(object @event)
        {
            return await Task.Run(() => Publish(@event));
        }

        public bool Publish(object @event)
        {
            Type type = @event.GetType();
            Func<object, bool> dispatcher = _byTypeDispatch[type] as Func<object, bool>;
            if (dispatcher == null)
            {
                dispatcher = BuildByTypeDispatcher(type);
                _byTypeDispatch[type] = dispatcher;
            }
            return dispatcher(@event);
        }

        private Func<object, bool> BuildByTypeDispatcher(Type superType)
        {
            const BindingFlags binding = BindingFlags.NonPublic | BindingFlags.Instance;
            MethodInfo publishTyped = typeof(Hub).GetTypeInfo().GetMethod(nameof(PublishTyped), binding);

            Dictionary<Type, MethodInfo> dispatchers = new Dictionary<Type, MethodInfo>
            {
                {superType, publishTyped.MakeGenericMethod(superType)}
            };

            foreach (var childType in _typeResolver.GetAncestors(superType))
                dispatchers.Add(childType, publishTyped.MakeGenericMethod(childType));

            Func<object, bool> function = @event =>
            {
                bool result = true;
                foreach (var dispatcher in dispatchers)
                {
                    MethodInfo method = dispatcher.Value;

                    bool handled = (bool)method.Invoke(this, new[] { @event });

                    result &= handled;
                }
                return result;
            };

            return function;
        }

        private bool PublishTyped<T>(T @event)
        {
            IDisposable subscription;
            Type subscriptionType = typeof(T);

            if (_subscriptions.TryGetValue(subscriptionType, out subscription))
            {
                ISubject<T> subject = (ISubject<T>)subscription;
                try
                {
                    subject.OnNext(@event);
                    return _results[subscriptionType];
                }
                catch (Exception ex)
                {
                    subject.OnError(ex); // <-- this kind of exception will cancel the observable sequence
                    _results[subscriptionType] = false;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Subscribes a manifold handler. This is required if a handler acts as a consumer for more than one event type. 
        /// </summary>
        public void Subscribe(object handler)
        {
            Type type = handler.GetType();
            Type[] interfaces = type.GetTypeInfo().GetInterfaces();
            IEnumerable<Type> consumers = interfaces.Where(i => typeof(IConsume<>).GetTypeInfo().IsAssignableFrom(i.GetGenericTypeDefinition()));

            const BindingFlags binding = BindingFlags.Instance | BindingFlags.NonPublic;
            MethodInfo subscribeByInterface = typeof(Hub).GetTypeInfo().GetMethod(nameof(SubscribeByInterface), binding);

            // void Subscribe<T>(IConsume<T> consumer)
            foreach (var consumer in consumers)
            {
                // SubscribeByInterface(consumer);
                Type handlerType = consumer.GetTypeInfo().GetGenericArguments()[0];
                subscribeByInterface.MakeGenericMethod(handlerType).Invoke(this, new[] { handler });
            }
        }

        public void Subscribe<T>(Action<T> handler)
        {
            SubscribeByDelegate(handler);
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
            IDisposable reference;
            _subscriptions.TryRemove(typeof(T), out reference);

            CancellationTokenSource cancel;
            if (_unsubscriptions.TryGetValue(typeof(T), out cancel))
                cancel.Cancel();
        }

        private void SubscribeByDelegate<T>(Action<T> handler)
        {
            IDisposable subscription = GetSubscriptionSubject<T>();
            ISubject<T> subject = (ISubject<T>)subscription;
            IObservable<T> observable = subject.AsObservable();

            SubscribeWithDelegate(handler, observable);
        }

        private void SubscribeByDelegateAndTopic<T>(Action<T> handler, Func<T, bool> topic)
        {
            IDisposable subscription = GetSubscriptionSubject<T>();
            ISubject<T> subject = (ISubject<T>)subscription;
            IObservable<T> observable = subject.AsObservable();

            SubscribeWithDelegate(handler, observable, topic);
        }

        private void SubscribeByInterface<T>(IConsume<T> consumer)
        {
            IDisposable subscription = GetSubscriptionSubject<T>();
            ISubject<T> subject = (ISubject<T>)subscription;
            IObservable<T> observable = subject.AsObservable();

            SubscribeWithInterface(consumer, observable);
        }

        private void SubscribeByInterfaceAndTopic<T>(IConsume<T> consumer, Func<T, bool> topic)
        {
            IDisposable subscription = GetSubscriptionSubject<T>();
            ISubject<T> subject = (ISubject<T>)subscription;
            IObservable<T> observable = subject.AsObservable();

            SubscribeWithInterface(consumer, observable, topic);
        }

        private void SubscribeWithDelegate<T>(Action<T> handler, IObservable<T> observable, Func<T, bool> filter = null)
        {
            var subscriptionType = typeof(T);
            var unsubscription = _unsubscriptions.GetOrAdd(subscriptionType, t => new CancellationTokenSource());

            if (filter != null)
            {
                observable.Subscribe(@event =>
                {
                    try
                    {
                        if (!filter(@event))
                            _results[subscriptionType] = false;
                        else
                        {
                            handler(@event);
                            _results[subscriptionType] = true;
                        }
                    }
                    catch (Exception)
                    {
                        _results[subscriptionType] = false;
                    }
                }, exception =>
                {
                    _results[subscriptionType] = false;
                }, () => { }, unsubscription.Token);
            }
            else
            {
                observable.Subscribe(@event =>
                {
                    try
                    {
                        handler(@event);
                        _results[subscriptionType] = true;
                    }
                    catch (Exception)
                    {
                        _results[subscriptionType] = false;
                    }
                }, exception =>
                {
                    _results[subscriptionType] = false;
                }, () => { }, unsubscription.Token);
            }
        }

        private void SubscribeWithInterface<T>(IConsume<T> consumer, IObservable<T> observable, Func<T, bool> filter = null)
        {
            var subscriptionType = typeof(T);
            var unsubscription = _unsubscriptions.GetOrAdd(subscriptionType, t => new CancellationTokenSource());

            if (filter != null)
            {
                observable.Subscribe(@event =>
                {
                    try
                    {
                        if (!filter(@event))
                            _results[subscriptionType] = false;
                        else
                            _results[subscriptionType] = consumer.HandleAsync(@event).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch (Exception)
                    {
                        _results[subscriptionType] = false;
                    }
                }, exception => { }, () =>
                {
                    _results[subscriptionType] = false;
                }, unsubscription.Token);
            }
            else
            {
                observable.Subscribe(@event =>
                {
                    try
                    {
                        _results[subscriptionType] = consumer.HandleAsync(@event).ConfigureAwait(false).GetAwaiter().GetResult();
                    }
                    catch
                    {
                        _results[subscriptionType] = false;
                    }
                }, exception => { }, () =>
                {
                    _results[subscriptionType] = false;
                }, unsubscription.Token);
            }
            
        }

        private IDisposable GetSubscriptionSubject<T>()
        {
            Type subscriptionType = typeof(T);

            return _subscriptions.GetOrAdd(subscriptionType, t =>
            {
                Subject<T> subject = new Subject<T>();

                return subject;
            });
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing || _subscriptions == null || _subscriptions.Count == 0)
                return;

            foreach (var subscription in _subscriptions.Where(subscription => subscription.Value != null))
            {
                IDisposable reference;
                if (_subscriptions.TryRemove(subscription.Key, out reference))
                {
                    CancellationTokenSource cancel;
                    if (_unsubscriptions.TryGetValue(subscription.Key, out cancel))
                        cancel.Cancel();

                    subscription.Value.Dispose();
                }
            }
        }
    }
}