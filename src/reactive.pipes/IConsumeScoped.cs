// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes
{
	/// <summary>
	///     Allows scoped programmability for a given consumer.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IConsumeScoped<in T> : IConsume<T>
	{
		/// <summary>
		///     Runs before handling a message.
		///     If this method returns <code>false</code>, the handler is not invoked.
		/// </summary>
		/// <returns></returns>
		bool Before();

		/// <summary>
		///     Runs after handling a message.
		///     The result returned fromm HandleAsync is replaced with the return of this method.
		/// </summary>
		/// <param name="result">The value returned from the handler after invoking.</param>
		/// <returns></returns>
		bool After(bool result);
	}
}