// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes
{
	/// <summary>
	///     A pipe produces on one end and consumers from another.
	/// </summary>
	/// <typeparam name="TP"></typeparam>
	/// <typeparam name="TC"></typeparam>
	public interface IPipe<out TP, in TC> : IProduce<TP>, IConsume<TC>
	{
	}
}