using Dates;

namespace reactive.pipes.scheduled
{
    public static class RepeatInfoExtensions
    {
        public static void RepeatIndefinitely(this ScheduledTask task, DatePeriod interval)
        {
            var start = task.RunAt;
            task.RepeatInfo = new RepeatInfo(start, interval);
        }

        public static void RepeatUntil(this ScheduledTask task, DatePeriod interval, DatePeriod until)
        {
            var start = task.RunAt;
            task.RepeatInfo = new RepeatInfo(start, interval) { EndPeriod = until };
        }
    }
}