// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;

namespace reactive.pipes
{
	/// <summary>
	///     An message publisher allows eventing to one or more consumers.
	/// </summary>
	public interface IMessagePublisher : IDisposable
	{
		Task<bool> PublishAsync(object message);
		bool Publish(object message);
	}
}