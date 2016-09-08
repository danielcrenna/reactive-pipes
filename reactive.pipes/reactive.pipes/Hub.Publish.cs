using System;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reflection;
using System.Threading.Tasks;

namespace reactive.pipes
{
    partial class Hub : IMessagePublisher
    {
        public async Task<bool> PublishAsync(object message)
        {
            return await Task.Run(() => Publish(message));
        }

        public bool Publish(object message)
        {
            Type type = message.GetType();
            Func<object, bool> dispatcher = _byTypeDispatch[type] as Func<object, bool>;
            if (dispatcher == null)
            {
                dispatcher = BuildByTypeDispatcher(type);
                _byTypeDispatch[type] = dispatcher;
            }
            return dispatcher(message);
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
    }
}
