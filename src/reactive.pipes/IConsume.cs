// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Threading.Tasks;

namespace reactive.pipes
{
	/// <summary>
	///     An event handler; contains the processing or storage logic for when an event is received
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public interface IConsume<in T>
	{
		Task<bool> HandleAsync(T message);
	}
}