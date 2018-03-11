using System;
using Newtonsoft.Json;

namespace reactive.pipes.scheduled
{
    public static class ScheduledProducerExtensions
    {
        /// <summary>
        /// Schedules a new task for delayed execution for the given producer.
        /// 
        /// If the user does NOT provide a RunAt during options, but an expression IS provided, the next occurrence of the expression, relative to now, will be selected as the start time.
        /// Otherwise, the task will be scheduled for now.
        /// 
        /// </summary>
        /// <typeparam name="T">The type of the task. The task is created at trigger time. The trigger type must have a parameterless constructor.</typeparam>
        /// <param name="options">Allows configuring task-specific features. Note that this is NOT invoked at invocation time lazily, but at scheduling time (i.e. immediately). </param>
        /// <param name="configure">Allows setting parameters on the scheduled task. Note that this is NOT invoked at invocation time lazily, but at scheduling time (i.e. immediately).</param>
        /// <returns>Whether the scheduled operation was successfull; if `true`, it was either scheduled or ran successfully, depending on configuration. If `false`, it either failed to schedule or failed during execution, depending on configuration.</returns>
        public static bool ScheduleAsync<T>(this ScheduledProducer producer, Action<ScheduledTask> options = null, Action<T> configure = null) where T : class, new()
        {
            T instance = null;

            if (configure != null)
            {
                instance = (T)Activator.CreateInstance(typeof(T));

                configure(instance);
            }

            return QueueForExecution<T>(producer, options, instance);
        }
        
        private static bool QueueForExecution<T>(this ScheduledProducer producer, Action<ScheduledTask> options, object instance)
        {
            var task = NewTask<T>(producer.Settings, instance);

            options?.Invoke(task); // <-- at this stage, task should have a RunAt set by the user or it will be default

            // Validate the CRON expression:
            if (!string.IsNullOrWhiteSpace(task.Expression) && !task.HasValidExpression)
                throw new ArgumentException("The provided CRON expression is invalid. Have you tried the CronTemplates?");

            // Handle when no start time is provided up front
            if (task.RunAt == default(DateTimeOffset))
            {
                task.RunAt = DateTimeOffset.UtcNow;

                if (task.NextOccurrence.HasValue)
                    task.RunAt = task.NextOccurrence.Value;
            }

            // Set the "Start" property only once, equal to the very first RunAt 
            task.Start = task.RunAt;

            if (!producer.Settings.DelayTasks)
                return producer.AttemptTask(task, false);
      
            producer.Settings.Store?.Save(task);
            return true;
        }

        private static ScheduledTask NewTask<T>(ScheduledProducerSettings settings, object instance = null)
        {
            Type type = typeof (T);
            HandlerInfo handlerInfo = new HandlerInfo(type.Namespace, type.Name);
            if (instance != null)
                handlerInfo.Instance = JsonConvert.SerializeObject(instance);

            ScheduledTask scheduledTask = new ScheduledTask
            {
                Handler = JsonConvert.SerializeObject(handlerInfo)
            };
            
            settings.ProvisionTask(scheduledTask);
            return scheduledTask;
        }
    }
}
