// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes
{
	public enum DispatchConcurrencyMode
	{
		/// <summary>
		///     Dispatch of messages is serial by subscription.
		/// </summary>
		Default,

		/// <summary>
		///     Dispatch of messages is asynchronous, and nearly certain to be out of insertion order.
		/// </summary>
		Unsafe
	}
}