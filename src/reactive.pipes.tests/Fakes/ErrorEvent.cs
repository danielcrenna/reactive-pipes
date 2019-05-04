// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace reactive.pipes.tests.Fakes
{
	public class ErrorEvent : BaseEvent
	{
		public ErrorEvent() => Error = true;

		public bool Error { get; set; }
	}
}