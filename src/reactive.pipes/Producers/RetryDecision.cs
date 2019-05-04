// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes.Producers
{
	public enum RetryDecision
	{
		RetryImmediately,
		Requeue,
		Backlog,
		Undeliverable,
		Destroy
	}
}