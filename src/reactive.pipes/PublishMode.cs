// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes
{
	public enum PublishMode
	{
		/// <summary>
		/// Publishing a message waits for the ACK from the consumer.
		/// </summary>
		Default,

		/// <summary>
		/// Publishing a message does not wait for an ACK from the consumer. All published messages are assumed delivered without retry.
		/// </summary>
		FireAndForget
	}
}