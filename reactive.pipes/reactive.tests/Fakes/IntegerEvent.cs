using System;

namespace reactive.tests.Fakes
{
    public class IntegerEvent : IEquatable<IntegerEvent>
    {
        public int Value { get; set; }

        public IntegerEvent() { }

        public IntegerEvent(int value)
        {
            Value = value;
        }

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