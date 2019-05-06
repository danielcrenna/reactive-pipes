// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reactive.Disposables;

namespace reactive.pipes
{
	public class CompositeSubscription<T> : IObservableWithOutcomes<T>, IDisposable, IEnumerable<IObservableWithOutcomes<T>>
	{
		private readonly List<IObservableWithOutcomes<T>> _subscriptions;

		public CompositeSubscription()
		{
			_subscriptions = new List<IObservableWithOutcomes<T>>();
		}

		public void Add(IObservableWithOutcomes<T> subscription)
		{
			_subscriptions.Add(subscription);
		}

		public void Dispose()
		{
			foreach(var subject in _subscriptions)
				(subject as IDisposable)?.Dispose();
		}

		public bool Handled
		{
			get
			{
				var handled = true;
				foreach (var subscription in _subscriptions)
					handled &= subscription.Handled;
				return handled;
			}
		}

		public ICollection<ObservableOutcome> Outcomes => new List<ObservableOutcome>();

		public void Clear()
		{
			foreach (var subscription in _subscriptions)
				subscription.Outcomes.Clear();
		}

		public void OnNext(T value)
		{
			foreach (var subject in _subscriptions)
			{
				subject.OnNext(value);
			}
		}

		public void OnError(Exception error)
		{
			foreach (var subject in _subscriptions)
			{
				subject.OnError(error);
			}
		}

		public void OnCompleted()
		{
			foreach (var subject in _subscriptions)
			{
				subject.OnCompleted();
			}
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			var composite = new CompositeDisposable();
			foreach (var subject in _subscriptions)
			{
				composite.Add(subject.Subscribe(observer));
			}
			return composite;
		}

		public IEnumerator<IObservableWithOutcomes<T>> GetEnumerator()
		{
			return _subscriptions.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}