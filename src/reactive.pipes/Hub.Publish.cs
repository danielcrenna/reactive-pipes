using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace reactive.pipes
{
    partial class Hub : IMessagePublisher
    {
        public Task<bool> PublishAsync(object message)
        {
            return Task.Run(() => Publish(message));
        }

        public bool Publish(object message)
        {
            var type = message.GetType();
	        if (!(_byTypeDispatch[type] is Func<object, bool> dispatcher))
            {
                dispatcher = BuildByTypeDispatcher(type);
                _byTypeDispatch[type] = dispatcher;
            }
            return dispatcher(message);
        }

        private Func<object, bool> BuildByTypeDispatcher(Type superType)
        {
            const BindingFlags binding = BindingFlags.NonPublic | BindingFlags.Instance;
            var publishTyped = typeof(Hub).GetTypeInfo().GetMethod(nameof(PublishTyped), binding);
	        Debug.Assert(publishTyped != null);

            var dispatchers = new Dictionary<Type, MethodInfo>
            {
                {superType, publishTyped?.MakeGenericMethod(superType)}
            };

            foreach (var childType in _typeResolver.GetAncestors(superType))
                dispatchers.Add(childType, publishTyped?.MakeGenericMethod(childType));

	        bool Dispatch(object @event)
	        {
		        var result = true;
		        foreach (var dispatcher in dispatchers)
		        {
			        var method = dispatcher.Value;
			        var handled = (bool) method.Invoke(this, new[] {@event});
			        result &= handled;
		        }
		        return result;
	        }

	        return Dispatch;
        }
        
        private bool PublishTyped<T>(T @event)
        {
	        var subscriptionType = typeof(T);

            if (_subscriptions.TryGetValue(subscriptionType, out var subscription))
            {
                lock (subscription)
                {
                    var subject = (WrappedSubject<T>)subscription;
                    try
                    {
                        subject.Outcomes.Clear();
                        subject.OnNext(@event);
                        return subject.Handled;
                    }
                    catch (Exception ex)
                    {
                        subject.OnError(ex); // <-- this kind of exception will cancel the observable sequence
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
