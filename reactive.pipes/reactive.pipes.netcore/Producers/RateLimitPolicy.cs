using System;

namespace reactive.pipes.Producers
{
    /// <summary>
    /// A rate limit policy, for use with <see cref="BackgroundThreadProducer{T}" />
    /// </summary>
    public class RateLimitPolicy
    {
        public bool Enabled { get; set; }
        public int Occurrences { get; set; }
        public TimeSpan TimeUnit { get; set; }
    }
}