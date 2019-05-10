// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading;
using reactive.pipes.Consumers;

namespace reactive.pipes
{
	/// <summary>
	///     Allows subscribing handlers to centralized publishing events, and allows subscription by topic.
	///     Basically, it is both an aggregator and a publisher, and is typically used to provide pub-sub services in-process.
	/// </summary>
	public partial class Hub : IMessageAggregator
	{
		private static readonly Action<Exception> NoopErrorHandler = e => { };
		private readonly Hashtable _byTypeDispatch = new Hashtable();

		private readonly ConcurrentDictionary<SubscriptionKey, IDisposable> _subscriptions =
			new ConcurrentDictionary<SubscriptionKey, IDisposable>();

		private readonly ITypeResolver _typeResolver;

		private readonly ConcurrentDictionary<SubscriptionKey, CancellationTokenSource> _unsubscriptions =
			new ConcurrentDictionary<SubscriptionKey, CancellationTokenSource>();

		public Hub() : this(GetDefaultAssemblies()) { }

		public Hub(IEnumerable<Assembly> assemblies) => _typeResolver = new DefaultTypeResolver(assemblies);

		public Hub(ITypeResolver typeResolver) => _typeResolver = typeResolver;

		/// <summary>
		///     Subscribes a manifold handler. This is required if a handler acts as a consumer for more than one message
		///     type.
		/// </summary>
		public void Subscribe(object handler, Action<Exception> onError = null)
		{
			var type = handler.GetType();
			var interfaces = type.GetInterfaces();
			var consumers = interfaces.Where(i => typeof(IConsume<>).IsAssignableFrom(i.GetGenericTypeDefinition()));
			
			// void Subscribe<T>(IConsume<T> consumer)
			foreach (var consumer in consumers)
			{
				// SubscribeByInterface(consumer);
				var handlerType = consumer.GetGenericArguments()[0];
				Constants.Methods.SubscribeByInterface.MakeGenericMethod(handlerType)
					.Invoke(this, new[] {handler, onError ?? NoopErrorHandler});
			}
		}

		public void Subscribe<T>(Action<T> handler, Action<Exception> onError = null)
		{
			SubscribeByDelegate(handler, onError ?? NoopErrorHandler);
		}

		public void Subscribe<T>(Action<T> handler, Func<T, bool> topic, Action<Exception> onError = null)
		{
			SubscribeByDelegateAndTopic(handler, topic, onError ?? NoopErrorHandler);
		}

		public void Subscribe<T>(IConsume<T> consumer, Action<Exception> onError = null)
		{
			SubscribeByInterface(consumer, onError ?? NoopErrorHandler);
		}

		public void Subscribe<T>(IConsume<T> consumer, Func<T, bool> topic, Action<Exception> onError = null)
		{
			SubscribeByInterfaceAndTopic(consumer, topic, onError ?? NoopErrorHandler);
		}

		public void Unsubscribe<T>(Func<T, bool> topic)
		{
			var key = SubscriptionKey.Create(topic);

			_subscriptions.TryRemove(key, out var disposable);

			if (_unsubscriptions.TryGetValue(key, out var cancel))
				cancel.Cancel();

			disposable.Dispose();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public static IEnumerable<Assembly> GetDefaultAssemblies()
		{
			var assemblies = new List<Assembly>();
			assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());
			return assemblies;
		}

		private void SubscribeByDelegate<T>(Action<T> handler, Action<Exception> onError)
		{
			SubscribeWithDelegate(handler, onError);
		}

		private void SubscribeByDelegateAndTopic<T>(Action<T> handler, Func<T, bool> topic, Action<Exception> onError)
		{
			SubscribeWithDelegate(handler, onError, topic);
		}

		private void SubscribeByInterface<T>(IConsume<T> consumer, Action<Exception> onError)
		{
			SubscribeWithInterface(consumer, onError);
		}

		private void SubscribeByInterfaceAndTopic<T>(IConsume<T> consumer, Func<T, bool> topic,
			Action<Exception> onError)
		{
			SubscribeWithInterface(consumer, onError, topic);
		}

		private void SubscribeWithDelegate<T>(Action<T> handler, Action<Exception> onError, Func<T, bool> topic = null)
		{
			SubscribeWithInterface(new ActionConsumer<T>(handler), onError, topic);
		}

		private void SubscribeWithInterface<T>(IConsume<T> consumer, Action<Exception> onError,
			Func<T, bool> topic = null)
		{
			var key = SubscriptionKey.Create(SubscriptionKeyMode == SubscriptionKeyMode.Topical ? topic : null);

			var getOrAdd = _subscriptions.GetOrAdd(key, k => new Subscription<T>(OutcomePolicy));
			var observable = (IObservableWithOutcomes<T>) getOrAdd;
			
			if (key.Topic != null)
			{
				// upgrade the "*" subscription to a composite subscription (messages don't know about topics)
				var noScope = SubscriptionKey.Create<T>(null);
				var maybeComposite = _subscriptions.GetOrAdd(noScope, k => new CompositeSubscription<T>());
			
				if(maybeComposite is CompositeSubscription<T> upgraded)
					upgraded.Add(observable);
				else
				{
					upgraded = new CompositeSubscription<T>();
					upgraded.Add((IObservableWithOutcomes<T>) maybeComposite);
					upgraded.Add(observable);

					_subscriptions[noScope] = upgraded;
				}
			}

			var unsubscription = _unsubscriptions.GetOrAdd(key, t => new CancellationTokenSource());

			var o = (IObservable<T>) observable;
			if (Scheduler != null)
				o = o.ObserveOn(Scheduler);

			if (topic != null)
			{
				o.Subscribe(message =>
				{
					try
					{
						if (!topic(message))
						{
							bool result;
							switch (TopicFilteredResult)
							{
								case TopicFilteredResult.Failure:
									result = false;
									break;
								case TopicFilteredResult.Success:
									result = true;
									break;
								default:
									throw new ArgumentOutOfRangeException();
							}
							observable.Outcomes.Add(new ObservableOutcome {Result = result});
						}
						else
						{
							var result = Handle(message);
							observable.Outcomes.Add(new ObservableOutcome {Result = result});
						}
					}
					catch (Exception ex)
					{
						OnHandlerError(onError, ex, observable);
					}
				}, e => OnError(e, observable), () => OnCompleted(observable), unsubscription.Token);
			}
			else
			{
				o.Subscribe(message =>
				{
					try
					{
						var result = Handle(message);
						observable.Outcomes.Add(new ObservableOutcome {Result = result});
					}
					catch (Exception ex)
					{
						OnHandlerError(onError, ex, observable);
					}
				}, e => OnError(e, observable), () => OnCompleted(observable), unsubscription.Token);
			}

			bool Handle(T message)
			{
				if (consumer is IConsumeScoped<T> before)
				{
					if (!before.Before(message))
					{
						if (consumer is ITreatFailureAsSuccess)
							return true; // ignore message

						return false;
					}
				}
				
				var result = consumer.HandleAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();

				if (consumer is IConsumeScoped<T> after)
					result = after.After(message, result);

				return result;
			}
		}

		/// <summary>
		///     The observable sequence has completed; technically, this should never happen unless disposing, so we should
		///     treat this similarly to a fault since we can't guarantee delivery.
		/// </summary>
		private static void OnCompleted<T>(IObservableWithOutcomes<T> observable)
		{
			observable.Outcomes.Add(new ObservableOutcome {Result = false});
		}

		/// <summary> An error has been caught coming from the user handler; we need to allow a hook to pipeline it. </summary>
		private static void OnHandlerError<T>(Action<Exception> a, Exception e, IObservableWithOutcomes<T> observable)
		{
			a(e);
			observable.Outcomes.Add(new ObservableOutcome {Error = e, Result = false});
		}

		/// <summary>
		///     The observable sequence faulted; technically this should never happen, since it would also cause observances
		///     to fail going forward on this thread, so we should treat this similarly to a fault, since delivery is shut down.
		/// </summary>
		private static void OnError<T>(Exception e, IObservableWithOutcomes<T> observable)
		{
			observable.Outcomes.Add(new ObservableOutcome {Error = e, Result = false});
		}
		
		protected virtual void Dispose(bool disposing)
		{
			if (!disposing || _subscriptions == null || _subscriptions.Count == 0)
				return;

			foreach (var subscription in _subscriptions.Where(subscription => subscription.Value != null))
			{
				if (!_subscriptions.TryRemove(subscription.Key, out var disposable))
					continue;

				if (_unsubscriptions.TryGetValue(subscription.Key, out var cancel))
					cancel.Cancel();

				disposable.Dispose();
			}
		}
	}
}