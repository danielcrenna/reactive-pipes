// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive.Linq;

namespace reactive.pipes.Producers
{
	/// <summary>
	///     A producer that emits the results of <see cref="IObservable{T}" /> sequences
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class ObservingProducer<T> : IProduce<T>, IDisposable
	{
		private readonly IDictionary<IObserver<T>, IObservable<T>> _cache;
		private readonly IList<IDisposable> _handles;
		private IConsume<T> _consumer;

		public ObservingProducer()
		{
			_cache = new ConcurrentDictionary<IObserver<T>, IObservable<T>>();
			_handles = new List<IDisposable>();
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Attach(IConsume<T> consumer)
		{
			_consumer = consumer;
		}

		public virtual ObservingProducer<T> Produces(IEnumerable<T> sequence, Action<Exception> onError = null,
			Action onCompleted = null)
		{
			return Produces(sequence.ToObservable(), onError, onCompleted);
		}

		public virtual ObservingProducer<T> Produces(IObservable<T> sequence, Action<Exception> onError = null,
			Action onCompleted = null)
		{
			var observer = new ActionObserver<T>(async @event => await _consumer.HandleAsync(@event),
				onError ?? (exception => { }), onCompleted ?? (() => { }));
			_cache.Add(observer, sequence);
			return this;
		}

		public void Start()
		{
			SubscribeFromCache();
		}

		private void SubscribeFromCache()
		{
			foreach (var item in _cache) _handles.Add(item.Value.Subscribe(item.Key));
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!disposing) return;
			foreach (var handle in _handles)
				handle.Dispose();
			_handles.Clear();
			_cache.Clear();
		}
	}
}