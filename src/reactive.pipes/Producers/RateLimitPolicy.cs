// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace reactive.pipes.Producers
{
	/// <summary>
	///     A rate limit policy, for use with <see cref="BackgroundThreadProducer{T}" />
	/// </summary>
	public class RateLimitPolicy
	{
		public bool Enabled { get; set; }
		public int Occurrences { get; set; }
		public TimeSpan TimeUnit { get; set; }
	}
}