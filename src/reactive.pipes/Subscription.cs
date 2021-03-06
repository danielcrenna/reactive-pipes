﻿// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;

namespace reactive.pipes
{
	public class Subscription<T> : ISubject<T>, IObservableWithOutcomes<T>, IDisposable
	{
		private readonly OutcomePolicy _policy;
		private readonly ISubject<T> _subject;

		public Subscription(OutcomePolicy policy)
		{
			_subject = new Subject<T>();
			_policy = policy;

			Outcomes = new List<ObservableOutcome>();
		}

		public void Dispose()
		{
			(_subject as IDisposable)?.Dispose();
		}

		public bool Handled
		{
			get
			{
				switch (_policy)
				{
					case OutcomePolicy.Pessimistic:
						return Outcomes.All(o => o.Result);
					case OutcomePolicy.Optimistic:
						return Outcomes.Any(o => o.Result);
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public ICollection<ObservableOutcome> Outcomes { get; }

		public void Clear()
		{
			Outcomes?.Clear();
		}

		public void OnNext(T value)
		{
			_subject.OnNext(value);
		}

		public void OnError(Exception error)
		{
			_subject.OnError(error);
		}

		public void OnCompleted()
		{
			_subject.OnCompleted();
		}

		public IDisposable Subscribe(IObserver<T> observer)
		{
			return _subject.Subscribe(observer);
		}
	}
}