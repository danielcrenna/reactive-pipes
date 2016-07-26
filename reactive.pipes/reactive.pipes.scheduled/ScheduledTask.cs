using System;
using System.Collections.Generic;
using System.Linq;
using NCrontab;

namespace reactive.pipes.scheduled
{
    public class ScheduledTask
    {
        public int Id { get; set; }
        public int Priority { get; set; }
        public int Attempts { get; set; }
        public string Handler { get; set; }
        public DateTimeOffset RunAt { get; set; }
        public TimeSpan? MaximumRuntime { get; set; }
        public int? MaximumAttempts { get; set; }
        public bool? DeleteOnSuccess { get; set; }
        public bool? DeleteOnFailure { get; set; }
        public bool? DeleteOnError { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string LastError { get; set; }
        public DateTimeOffset? FailedAt { get; set; }
        public DateTimeOffset? SucceededAt { get; set; }
        public DateTimeOffset? LockedAt { get; set; }
        public string LockedBy { get; set; }

        #region Scheduling

        public string Expression { get; set; }
        public DateTimeOffset Start { get; set; }
        public DateTimeOffset? End { get; set; }
        public bool ContinueOnSuccess { get; set; } = true;
        public bool ContinueOnFailure { get; set; }
        public bool ContinueOnError { get; set; }
        public DateTimeOffset? NextOccurrence => GetNextOccurence();
        public DateTimeOffset? LastOccurrence => GetLastOccurrence();
        public IEnumerable<DateTimeOffset> AllOccurrences => GetAllOccurrences();

        private IEnumerable<DateTimeOffset> GetAllOccurrences()
        {
            if (string.IsNullOrWhiteSpace(Expression))
                return Enumerable.Empty<DateTimeOffset>();

            if (!End.HasValue)
                throw new ArgumentException("You cannot request all occurrences of an infinite series", nameof(End));

            return GetFiniteSeriesOccurrences(End.Value);
        }

        private DateTimeOffset? GetNextOccurence()
        {
            if (string.IsNullOrWhiteSpace(Expression))
                return null;

            // important: never iterate occurrences, the series could be inadvertently huge (i.e. 100 years of seconds)
            return End == null ? GetNextOccurrenceInInfiniteSeries() : GetFiniteSeriesOccurrences(End.Value).FirstOrDefault();
        }

        private DateTimeOffset? GetLastOccurrence()
        {
            if (string.IsNullOrWhiteSpace(Expression))
                return null;

            if (!End.HasValue)
                throw new ArgumentException("You cannot request the last occurrence of an infinite series", nameof(End));

            return GetFiniteSeriesOccurrences(End.Value).Last();
        }

        private DateTimeOffset? GetNextOccurrenceInInfiniteSeries()
        {
            CrontabSchedule schedule = TryParseCron();
            if (schedule == null)
                return null;

            DateTime nextOccurrence = schedule.GetNextOccurrence(Start.DateTime.ToLocalTime());

            return new DateTimeOffset(nextOccurrence.ToUniversalTime());
        }

        private IEnumerable<DateTimeOffset> GetFiniteSeriesOccurrences(DateTimeOffset end)
        {
            CrontabSchedule schedule = TryParseCron();
            if (schedule == null)
                return Enumerable.Empty<DateTimeOffset>();

            var occurrences = schedule.GetNextOccurrences(Start.DateTime, end.DateTime).Select(o => new DateTimeOffset(o.ToUniversalTime()));
            return occurrences;
        }

        private CrontabSchedule TryParseCron()
        {
            if (string.IsNullOrWhiteSpace(Expression))
                return null;

            return CrontabSchedule.TryParse(Expression);
        }

        #endregion
    }
}