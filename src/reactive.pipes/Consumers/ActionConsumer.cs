// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace reactive.pipes.Consumers
{
	/// <summary>
	///     A consumer that executes a delegate against any received events.
	///     It can also optionally forward to another consumer after invoking the delegate action.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public sealed class ActionConsumer<T> : IConsumeScoped<T>
	{
		private readonly Func<T, bool> _before;
		private readonly Func<T, bool, bool> _after;
		private readonly Func<T, Task<bool>> _delegate;

		public ActionConsumer(Action<T> @delegate, IConsume<T> forwardTo, Func<T, bool> before = null, Func<T, bool, bool> after = null)
		{
			_before = before;
			_after = after;
			_delegate = async @event =>
			{
				@delegate(@event);

				return await forwardTo.HandleAsync(@event);
			};
		}

		public ActionConsumer(Action<T> @delegate, Func<T, bool> before = null, Func<T, bool, bool> after = null)
		{
			_before = before;
			_after = after;
			_delegate = @event =>
			{
				@delegate(@event);
				return Task.FromResult(true);
			};
		}

		public ActionConsumer(Func<T, Task<bool>> @delegate) => _delegate = @delegate;

		public Task<bool> HandleAsync(T message)
		{
			return _delegate(message);
		}

		public bool Before(T message)
		{
			return _before == null || _before(message);
		}

		public bool After(T message, bool result)
		{
			return _after?.Invoke(message, result) ?? result;
		}
	}
}