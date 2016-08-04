using System;
using Newtonsoft.Json;

namespace reactive.pipes.Scheduler
{
    public static class ScheduledProducerExtensions
    {
        /// <summary>
        /// Schedules a new task for delayed execution for the given producer.
        /// </summary>
        /// <typeparam name="T">The type of the task. The task is created at trigger time. The trigger type must have a parameterless constructor.</typeparam>
        /// <param name="runAt">The time to execute the task. If the task repeats, the first occurrence. If no value is provided, the next occurence is used, or, the task is queued immediately if the task does not repeat.</param>
        /// <param name="options">Allows configuring task-specific features. Note that this is NOT invoked at invocation time lazily, but at scheduling time (i.e. immediately). </param>
        /// <param name="configure">Allows setting parameters on the scheduled task. Note that this is NOT invoked at invocation time lazily, but at scheduling time (i.e. immediately).</param>
        /// <returns></returns>
        public static bool ScheduleAsync<T>(this ScheduledProducer producer, DateTimeOffset? runAt = null, Action<ScheduledTask> options = null, Action<T> configure = null) where T : class, new()
        {
            T instance = null;

            if (configure != null)
            {
                instance = (T)Activator.CreateInstance(typeof(T));
                configure(instance);
            }

            return QueueForExecution<T>(producer, runAt, options, instance);
        }
        
        private static bool QueueForExecution<T>(this ScheduledProducer producer, DateTimeOffset? runAt, Action<ScheduledTask> options, object instance)
        {
            var task = NewTask<T>(runAt, producer.Settings, instance);
            options?.Invoke(task);

            if (!producer.Settings.DelayTasks)
                return producer.AttemptTask(task, false);
      
            producer.Settings.Store?.Save(task);
            return true;
        }

        private static ScheduledTask NewTask<T>(DateTimeOffset? runAt, ScheduledProducerSettings settings, object instance = null)
        {
            Type type = typeof (T);
            HandlerInfo handlerInfo = new HandlerInfo(type.Namespace, type.Name);
            if (instance != null)
                handlerInfo.Instance = JsonConvert.SerializeObject(instance);

            ScheduledTask scheduledTask = new ScheduledTask
            {
                Handler = JsonConvert.SerializeObject(handlerInfo)
            };

            // If we have repeat info and don't provide initial RunAt, defer to next occurrence; 
            //    otherwise, do it now (since we are provided no other alternative)

            scheduledTask.RunAt = runAt ?? (scheduledTask.NextOccurrence ?? DateTimeOffset.UtcNow);

            settings.ProvisionTask(scheduledTask);
            return scheduledTask;
        }
    }
}
