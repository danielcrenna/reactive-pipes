// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace reactive.pipes.tests.Fakes
{
	public class IntegerEvent : IEquatable<IntegerEvent>
	{
		public IntegerEvent()
		{
		}

		public IntegerEvent(int value) => Value = value;

		public int Value { get; set; }

		public bool Equals(IntegerEvent other)
		{
			if (other == null) return false;
			return other.Value.Equals(Value);
		}

		public override string ToString()
		{
			return Value.ToString();
		}
	}
}