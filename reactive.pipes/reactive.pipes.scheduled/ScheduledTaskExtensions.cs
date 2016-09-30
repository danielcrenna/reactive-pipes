using System;

namespace reactive.pipes.scheduled
{
    public static class ScheduledTaskExtensions
    {
        public static void RepeatIndefinitely(this ScheduledTask task, string expression)
        {
            task.Expression = expression;
        }

        public static void RepeatUntil(this ScheduledTask task, string expression, DateTimeOffset end)
        {
            task.Expression = expression;
            task.End = end;
        }
    }
}