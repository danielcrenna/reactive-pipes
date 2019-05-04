// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace reactive.pipes
{
	public class ObservableOutcome
	{
		public bool Result { get; set; }
		public Exception Error { get; set; }
	}
}