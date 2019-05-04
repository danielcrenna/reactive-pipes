// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.IO;

namespace reactive.pipes
{
	/// <summary>
	///     A contract for protocol negotiation between produces and consumers.
	/// </summary>
	public interface ISerializer
	{
		Stream SerializeToStream<T>(T @event);
		T DeserializeFromStream<T>(Stream stream);
	}
}