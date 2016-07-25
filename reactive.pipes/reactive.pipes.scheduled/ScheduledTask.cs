using System;
using System.Collections.Generic;

namespace reactive.pipes.scheduled
{
    public class ScheduledTask : IEquatable<ScheduledTask>
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
        public RepeatInfo RepeatInfo { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public string LastError { get; set; }
        public DateTimeOffset? FailedAt { get; set; }
        public DateTimeOffset? SucceededAt { get; set; }
        public DateTimeOffset? LockedAt { get; set; }
        public string LockedBy { get; set; }

        #region Equality

        public bool Equals(ScheduledTask other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Id == other.Id && Priority == other.Priority && Attempts == other.Attempts &&
                   string.Equals(Handler, other.Handler) && string.Equals(LastError, other.LastError) &&
                   RunAt.Equals(other.RunAt) && FailedAt.Equals(other.FailedAt) && SucceededAt.Equals(other.SucceededAt) &&
                   LockedAt.Equals(other.LockedAt) && string.Equals(LockedBy, other.LockedBy) &&
                   CreatedAt.Equals(other.CreatedAt) &&
                   Equals(RepeatInfo, other.RepeatInfo) && MaximumAttempts == other.MaximumAttempts &&
                   MaximumRuntime.Equals(other.MaximumRuntime) && DeleteOnSuccess == other.DeleteOnSuccess &&
                   DeleteOnFailure == other.DeleteOnFailure && DeleteOnError == other.DeleteOnError;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ScheduledTask) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Id;
                hashCode = (hashCode*397) ^ Priority;
                hashCode = (hashCode*397) ^ Attempts;
                hashCode = (hashCode*397) ^ (Handler?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (LastError?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ RunAt.GetHashCode();
                hashCode = (hashCode*397) ^ FailedAt.GetHashCode();
                hashCode = (hashCode*397) ^ SucceededAt.GetHashCode();
                hashCode = (hashCode*397) ^ LockedAt.GetHashCode();
                hashCode = (hashCode*397) ^ (LockedBy?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ CreatedAt.GetHashCode();
                hashCode = (hashCode*397) ^ (RepeatInfo?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ MaximumAttempts.GetHashCode();
                hashCode = (hashCode*397) ^ MaximumRuntime.GetHashCode();
                hashCode = (hashCode*397) ^ DeleteOnSuccess.GetHashCode();
                hashCode = (hashCode*397) ^ DeleteOnFailure.GetHashCode();
                hashCode = (hashCode*397) ^ DeleteOnError.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ScheduledTask left, ScheduledTask right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ScheduledTask left, ScheduledTask right)
        {
            return !Equals(left, right);
        }

        private sealed class ScheduledTaskEqualityComparer : IEqualityComparer<ScheduledTask>
        {
            public bool Equals(ScheduledTask x, ScheduledTask y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;

                return x.Id == y.Id && x.Priority == y.Priority && x.Attempts == y.Attempts &&
                       string.Equals(x.Handler, y.Handler) && string.Equals(x.LastError, y.LastError) &&
                       x.RunAt.Equals(y.RunAt) && x.FailedAt.Equals(y.FailedAt) && x.SucceededAt.Equals(y.SucceededAt) &&
                       x.LockedAt.Equals(y.LockedAt) && string.Equals(x.LockedBy, y.LockedBy) &&
                       x.CreatedAt.Equals(y.CreatedAt) &&
                       Equals(x.RepeatInfo, y.RepeatInfo) && x.MaximumAttempts == y.MaximumAttempts &&
                       x.MaximumRuntime.Equals(y.MaximumRuntime) && x.DeleteOnSuccess == y.DeleteOnSuccess &&
                       x.DeleteOnFailure == y.DeleteOnFailure && x.DeleteOnError == y.DeleteOnError;
            }

            public int GetHashCode(ScheduledTask obj)
            {
                unchecked
                {
                    var hashCode = obj.Id;
                    hashCode = (hashCode*397) ^ obj.Priority;
                    hashCode = (hashCode*397) ^ obj.Attempts;
                    hashCode = (hashCode*397) ^ (obj.Handler != null ? obj.Handler.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ (obj.LastError != null ? obj.LastError.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ obj.RunAt.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.FailedAt.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.SucceededAt.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.LockedAt.GetHashCode();
                    hashCode = (hashCode*397) ^ (obj.LockedBy != null ? obj.LockedBy.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ obj.CreatedAt.GetHashCode();
                    hashCode = (hashCode*397) ^ (obj.RepeatInfo != null ? obj.RepeatInfo.GetHashCode() : 0);
                    hashCode = (hashCode*397) ^ obj.MaximumAttempts.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.MaximumRuntime.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.DeleteOnSuccess.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.DeleteOnFailure.GetHashCode();
                    hashCode = (hashCode*397) ^ obj.DeleteOnError.GetHashCode();
                    return hashCode;
                }
            }
        }

        public static IEqualityComparer<ScheduledTask> ScheduledTaskComparer { get; } = new ScheduledTaskEqualityComparer();

        #endregion
    }
}