// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace reactive.pipes.tests.Fakes
{
	public class StringEvent : IEquatable<StringEvent>
	{
		public StringEvent()
		{
		}

		public StringEvent(string text) => Text = text;

		public Guid PartitionKey { get; set; }

		public string Text { get; set; }

		public bool Equals(StringEvent other)
		{
			if (other == null) return false;
			return Text != null && other.Text.Equals(Text);
		}

		public override string ToString()
		{
			return Text;
		}
	}
}