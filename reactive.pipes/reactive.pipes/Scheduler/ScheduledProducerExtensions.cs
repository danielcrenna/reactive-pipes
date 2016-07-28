using System;
using Newtonsoft.Json;

namespace reactive.pipes.Scheduler
{
    public static class ScheduledProducerExtensions
    {
        public static bool ScheduleAsync<T>(this ScheduledProducer producer, DateTimeOffset? runAt = null, int? priority = null, Action<ScheduledTask> options = null, Action<T> configure = null) where T : class, new()
        {
            T instance = null;
            if (configure != null)
            {
                instance = (T)Activator.CreateInstance(typeof(T));
                configure(instance);
            }

            return QueueForExecution<T>(producer, runAt ?? DateTimeOffset.UtcNow, priority ?? producer.Settings.Priority, options, instance);
        }
        
        private static bool QueueForExecution<T>(this ScheduledProducer producer, DateTimeOffset runAt, int priority, Action<ScheduledTask> options, object instance)
        {
            var task = NewTask<T>(runAt, priority, producer.Settings, instance);
            options?.Invoke(task);

            if (!producer.Settings.DelayTasks)
                return producer.AttemptTask(task, false);
      
            producer.Settings.Store?.Save(task);
            return true;
        }

        private static ScheduledTask NewTask<T>(DateTimeOffset runAt, int priority, ScheduledProducerSettings settings, object instance = null)
        {
            Type type = typeof (T);
            HandlerInfo handlerInfo = new HandlerInfo(type.Namespace, type.Name);
            if (instance != null)
                handlerInfo.Instance = JsonConvert.SerializeObject(instance);

            ScheduledTask scheduledTask = new ScheduledTask
            {
                Priority = priority,
                Handler = JsonConvert.SerializeObject(handlerInfo),
                RunAt = runAt
            };
            settings.ProvisionTask(scheduledTask);
            return scheduledTask;
        }
    }
}
