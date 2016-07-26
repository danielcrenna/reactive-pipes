using System;
using Newtonsoft.Json;

namespace reactive.pipes.Scheduler
{
    public static class ScheduledProducerExtensions
    {
        public static bool ScheduleAsync<T>(this ScheduledProducer producer, DateTimeOffset? runAt = null, int? priority = null, Action<ScheduledTask> options = null)
        {
            return QueueForExecution<T>(producer, runAt ?? DateTimeOffset.UtcNow, priority ?? producer.Settings.Priority, options);
        }

        private static bool QueueForExecution<T>(this ScheduledProducer producer, DateTimeOffset runAt, int priority, Action<ScheduledTask> options)
        {
            var task = NewTask<T>(runAt, priority, producer.Settings);
            options?.Invoke(task);

            if (!producer.Settings.DelayTasks)
                return producer.AttemptTask(task, false);
      
            producer.Settings.Store?.Save(task);
            return true;
        }

        private static ScheduledTask NewTask<T>(DateTimeOffset runAt, int priority, ScheduledProducerSettings settings)
        {
            Type type = typeof (T);
            ScheduledTask scheduledTask = new ScheduledTask
            {
                Priority = priority,
                Handler = JsonConvert.SerializeObject(new HandlerInfo(type.Namespace, type.Name)),
                RunAt = runAt
            };
            settings.ProvisionTask(scheduledTask);
            return scheduledTask;
        }
    }
}
