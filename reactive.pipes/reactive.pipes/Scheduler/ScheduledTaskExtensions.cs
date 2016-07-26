using System;

namespace reactive.pipes.Scheduler
{
    public static class ScheduledTaskExtensions
    {
        public static void RepeatIndefinitely(this ScheduledTask task, string expression)
        {
            task.Expression = expression;
            task.Start = task.RunAt;
        }

        public static void RepeatUntil(this ScheduledTask task, string expression, DateTimeOffset end)
        {
            task.Expression = expression;
            task.Start = task.RunAt;
            task.End = end;
        }
    }
}