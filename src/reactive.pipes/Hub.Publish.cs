// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace reactive.pipes
{
	partial class Hub : IMessagePublisher
	{
		public DispatchConcurrencyMode DispatchConcurrencyMode = 0;
		public OutcomePolicy OutcomePolicy = 0;
		public SubscriptionKeyMode SubscriptionKeyMode = 0;
		public PublishMode PublishMode = PublishMode.Default;
		public TopicFilteredResult TopicFilteredResult = TopicFilteredResult.Failure;
		public IScheduler Scheduler = null;

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

			if (!_subscriptions.TryGetValue(key, out var disposable))
				return true;

			switch (DispatchConcurrencyMode)
			{
				case DispatchConcurrencyMode.Default:
					if (disposable is CompositeSubscription<T> composite)
					{
						foreach (var subscription in composite)
						{
							lock (subscription)
								ObserveOnSubscription(subscription);
						}
						return composite.Handled;
					}
					else
					{
						lock (disposable)
							return ObserveOnSubscription((IObservableWithOutcomes<T>) disposable);
					}
				case DispatchConcurrencyMode.Unsafe:
					return ObserveOnSubscription((IObservableWithOutcomes<T>) disposable);
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			bool ObserveOnSubscription(IObservableWithOutcomes<T> subscription)
			{
				switch (PublishMode)
				{
					case PublishMode.Default:
						return ObserveOne(subscription);
					case PublishMode.FireAndForget:
						var thread = new Thread(() => { ObserveOne(subscription); });
						thread.Start();
						return true;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			bool ObserveOne(IObservableWithOutcomes<T> subscription)
			{
				try
				{
					subscription.Clear();
					subscription.OnNext(message);
					return subscription.Handled;
				}
				catch (Exception ex)
				{
					subscription.OnError(ex); // <-- this kind of exception will cancel the observable sequence
					return false;
				}
			}
		}
	}
}