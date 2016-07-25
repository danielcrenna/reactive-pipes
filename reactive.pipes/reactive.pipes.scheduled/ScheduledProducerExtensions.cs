using System;
using Newtonsoft.Json;

namespace reactive.pipes.scheduled
{
    public static class ScheduledProducerExtensions
    {
        public static bool ScheduleAsync<T>(this ScheduledProducer producer, DateTimeOffset? runAt = null, int? priority = null, Action<ScheduledTask> options = null)
        {
            return QueueForExecution<T>(producer, runAt ?? DateTimeOffset.UtcNow);
        }

        private static bool QueueForExecution<T>(this ScheduledProducer producer, DateTimeOffset runAt, int? priority = null, Action<ScheduledTask> options = null)
        {
            var task = NewTask<T>(priority, runAt, producer.Settings);
            options?.Invoke(task);
            if (!producer.Settings.DelayTasks)
                return producer.AttemptTask(task, persist: false);

            producer.Settings.Store?.Save(task);
            return true;
        }

        private static ScheduledTask NewTask<T>(int? priority, DateTimeOffset runAt, ScheduledProducerSettings settings)
        {
            Type type = typeof (T);
            ScheduledTask scheduledTask = new ScheduledTask
            {
                Priority = priority ?? settings.Priority,
                Handler = JsonConvert.SerializeObject(new HandlerInfo(type.Namespace, type.Name)),
                RunAt = runAt
            };
            settings.ProvisionTask(scheduledTask);
            return scheduledTask;
        }
    }
}
