// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace reactive.pipes
{
	/// <summary>
	///     A message aggregator allows for topical subscriptions to some source.
	/// </summary>
	public interface IMessageAggregator
	{
		void Subscribe(object handler, Action<Exception> onError = null);
		void Subscribe<T>(Action<T> handler, Action<Exception> onError = null);
		void Subscribe<T>(Action<T> handler, Func<T, bool> topic, Action<Exception> onError = null);
		void Subscribe<T>(IConsume<T> consumer, Action<Exception> onError = null);
		void Subscribe<T>(IConsume<T> consumer, Func<T, bool> topic, Action<Exception> onError = null);
		void Unsubscribe<T>(Func<T, bool> topic);
	}
}