using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

namespace reactive.pipes
{
	partial class Hub : IMessagePublisher
	{
		public DispatchConcurrencyMode DispatchConcurrencyMode = 0;

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
            var dispatchers = new Dictionary<Type, MethodInfo>
            {
                {superType, Constants.Methods.PublishTyped.MakeGenericMethod(superType)}
            };

            foreach (var childType in _typeResolver.GetAncestors(superType))
                dispatchers.Add(childType, Constants.Methods.PublishTyped.MakeGenericMethod(childType));

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
        
        private bool PublishTyped<T>(T message)
        {
	        var key = SubscriptionKey.Create<T>(null);

	        if (!_subscriptions.TryGetValue(key, out var subscription))
		        return true;

	        switch (DispatchConcurrencyMode)
            {
	            case DispatchConcurrencyMode.Default:
		            lock (subscription)
						return ObserveOnSubject();
				case DispatchConcurrencyMode.Unsafe:
		            return ObserveOnSubject();
	            default:
		            throw new ArgumentOutOfRangeException();
            }
			
            bool ObserveOnSubject()
            {
	            var subject = (WrappedSubject<T>) subscription;
	            try
	            {
		            subject.Outcomes.Clear();
		            subject.OnNext(message);
		            return subject.Handled;
	            }
	            catch (Exception ex)
	            {
		            subject.OnError(ex); // <-- this kind of exception will cancel the observable sequence
		            return false;
	            }
            }
        }
    }
}
