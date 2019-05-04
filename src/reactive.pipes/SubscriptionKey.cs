// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace reactive.pipes
{
	internal struct SubscriptionKey : IEquatable<SubscriptionKey>
	{
		public Type Type { get; }
		public object Topic { get; }

		private SubscriptionKey(Type type, object topic)
		{
			Type = type;
			Topic = topic;
		}

		public static SubscriptionKey Create<T>(Func<T, bool> topic)
		{
			var key = new SubscriptionKey(typeof(T), topic);
			return key;
		}

		public bool Equals(SubscriptionKey other)
		{
			return Type == other.Type && Equals(Topic, other.Topic);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is SubscriptionKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((Type != null ? Type.GetHashCode() : 0) * 397) ^ (Topic != null ? Topic.GetHashCode() : 0);
			}
		}

		public static bool operator ==(SubscriptionKey left, SubscriptionKey right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(SubscriptionKey left, SubscriptionKey right)
		{
			return !left.Equals(right);
		}

		private sealed class TypeTopicEqualityComparer : IEqualityComparer<SubscriptionKey>
		{
			public bool Equals(SubscriptionKey x, SubscriptionKey y)
			{
				return x.Type == y.Type && Equals(x.Topic, y.Topic);
			}

			public int GetHashCode(SubscriptionKey obj)
			{
				unchecked
				{
					return ((obj.Type != null ? obj.Type.GetHashCode() : 0) * 397) ^
					       (obj.Topic != null ? obj.Topic.GetHashCode() : 0);
				}
			}
		}

		public static IEqualityComparer<SubscriptionKey> TypeTopicComparer { get; } = new TypeTopicEqualityComparer();
	}
}