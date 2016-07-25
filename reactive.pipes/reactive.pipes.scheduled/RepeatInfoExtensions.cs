using Dates;

namespace reactive.pipes.scheduled
{
    public static class RepeatInfoExtensions
    {
        public static void RepeatIndefinitely(this ScheduledTask task, DatePeriod interval)
        {
            task.RepeatInfo = new RepeatInfo(task.RunAt, interval);
        }

        public static void RepeatUntil(this ScheduledTask task, DatePeriod interval, DatePeriod until)
        {
            task.RepeatInfo = new RepeatInfo(task.RunAt, interval) { EndPeriod = until };
        }
    }
}