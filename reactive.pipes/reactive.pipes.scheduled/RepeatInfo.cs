using System;
using System.Collections.Generic;
using System.Linq;
using Dates;

namespace reactive.pipes.scheduled
{
    public class RepeatInfo : IEquatable<RepeatInfo>
    {
        private static readonly IDictionary<int, IEnumerable<DateTimeOffset>> OccurrencesCache = new Dictionary<int, IEnumerable<DateTimeOffset>>();
        
        public DatePeriod Period { get; set; }
        public DatePeriod? EndPeriod { get; set; }
        public DateTimeOffset Start { get; set; }
        public bool IncludeWeekends { get; set; }
        public bool ContinueOnSuccess { get; set; }
        public bool ContinueOnFailure { get; set; }
        public bool ContinueOnError { get; set; }

        public IEnumerable<DateTimeOffset> AllOccurrences => GetSeriesOccurrences();
        public DatePeriodFrequency PeriodFrequency => Period.Frequency;
        public int PeriodQuantifier => Period.Quantifier;
        public DatePeriodFrequency? EndPeriodFrequency => EndPeriod?.Frequency;
        public int? EndPeriodQuantifier => EndPeriod?.Quantifier;

        public DateTimeOffset? NextOccurrence => GetNextOccurence();
        public DateTimeOffset? LastOccurrence => EndPeriod.HasValue ? GetSeriesOccurrences().Last() : (DateTimeOffset?)null;


        internal RepeatInfo(DateTimeOffset start, DatePeriod period)
        {
            Start = start;
            Period = period;
            
            ContinueOnSuccess = true;
            ContinueOnFailure = false;
            ContinueOnError = false;
        }
        
        private DateTimeOffset? GetNextOccurence()
        {
            if (!EndPeriod.HasValue)
            {
                return GetSeriesOccurrences(new DatePeriod(DatePeriodFrequency.Years, 100)).FirstOrDefault();
            }
            var occurrence = GetSeriesOccurrences().FirstOrDefault();
            return occurrence;
        }

        private IEnumerable<DateTimeOffset> GetSeriesOccurrences(DatePeriod? endPeriod = null)
        {
            endPeriod = endPeriod ?? EndPeriod;
            if (!endPeriod.HasValue)
            {
                throw new ArgumentException("You cannot request the occurrences for an infinite series");
            }

            IEnumerable<DateTimeOffset> occurrences;
            if (OccurrencesCache.TryGetValue(GetHashCode(), out occurrences))
                return occurrences;

            DateTimeOffset start = Start;
            DateTimeOffset end;

            // Get the last occurrence
            switch (endPeriod.Value.Frequency)
            {
                case DatePeriodFrequency.Years:
                    end = start.AddYears(endPeriod.Value.Quantifier);
                    break;
                case DatePeriodFrequency.Weeks:
                    end = start.AddDays(endPeriod.Value.Quantifier * 7);
                    break;
                case DatePeriodFrequency.Days:
                    end = start.AddDays(endPeriod.Value.Quantifier);
                    break;
                case DatePeriodFrequency.Hours:
                    end = start.AddHours(endPeriod.Value.Quantifier);
                    break;
                case DatePeriodFrequency.Minutes:
                    end = start.AddHours(endPeriod.Value.Quantifier);
                    break;
                case DatePeriodFrequency.Seconds:
                    end = start.AddSeconds(endPeriod.Value.Quantifier);
                    break;
                case DatePeriodFrequency.Months:
                    end = start.AddMonths(endPeriod.Value.Quantifier);
                    break;
                default:
                    throw new ArgumentException("DatePeriodFrequency");
            }

            occurrences = Period.GetOccurrences(start, end, !IncludeWeekends).ToList();
            OccurrencesCache.Add(GetHashCode(), occurrences);
            return occurrences;
        }

        #region Equality

        public bool Equals(RepeatInfo other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Period.Equals(other.Period) && EndPeriod.Equals(other.EndPeriod) && Start.Equals(other.Start) &&
                   IncludeWeekends == other.IncludeWeekends && ContinueOnSuccess == other.ContinueOnSuccess &&
                   ContinueOnFailure == other.ContinueOnFailure && ContinueOnError == other.ContinueOnError;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RepeatInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Period.GetHashCode();
                hashCode = (hashCode*397) ^ EndPeriod.GetHashCode();
                hashCode = (hashCode*397) ^ Start.GetHashCode();
                hashCode = (hashCode*397) ^ IncludeWeekends.GetHashCode();
                hashCode = (hashCode*397) ^ ContinueOnSuccess.GetHashCode();
                hashCode = (hashCode*397) ^ ContinueOnFailure.GetHashCode();
                hashCode = (hashCode*397) ^ ContinueOnError.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(RepeatInfo left, RepeatInfo right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RepeatInfo left, RepeatInfo right)
        {
            return !Equals(left, right);
        }

        private sealed class RepeatInfoEqualityComparer : IEqualityComparer<RepeatInfo>
        {
            public bool Equals(RepeatInfo x, RepeatInfo y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return x.Period.Equals(y.Period) && x.EndPeriod.Equals(y.EndPeriod) && x.Start.Equals(y.Start) && x.IncludeWeekends == y.IncludeWeekends && x.ContinueOnSuccess == y.ContinueOnSuccess && x.ContinueOnFailure == y.ContinueOnFailure && x.ContinueOnError == y.ContinueOnError;
            }

            public int GetHashCode(RepeatInfo obj)
            {
                unchecked
                {
                    var hashCode = obj.Period.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.EndPeriod.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.Start.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.IncludeWeekends.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.ContinueOnSuccess.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.ContinueOnFailure.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.ContinueOnError.GetHashCode();
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<RepeatInfo> RepeatInfoComparer { get; } = new RepeatInfoEqualityComparer();

        #endregion
    }
}