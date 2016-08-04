using System;

namespace reactive.pipes.Scheduler
{
    public class ScheduledProducerSettings
    {
        /// <summary>
        /// Whether or not tasks are delayed or executed immediately; default is true
        /// </summary>
        public bool DelayTasks { get; set; }

        /// <summary>
        /// The backend used to find handler type info; default is built-in reflection.
        /// </summary>
        public ITypeResolver TypeResolver { get; set; }
        
        /// <summary>
        /// The backend used to coordinate scheduling; default is in-memory.
        /// </summary>
        public IScheduleStore Store { get; set; }

        /// <summary>
        /// The time to delay before checking for available tasks in the backing store. Default is 60 seconds.
        /// </summary>
        public TimeSpan SleepInterval { get; set; }

        /// <summary>
        /// The time to delay before checking for hanging tasks in the backing store. Default is 5 minutes.
        /// </summary>
        public TimeSpan CleanupInterval { get; set; }

        /// <summary>
        /// The number of threads available for performing tasks; default is 0.
        /// 
        /// A value of 0 defaults to the number of logical processors.
        /// </summary>
        public int Concurrency { get; set; }

        /// <summary>
        /// The number of jobs to pull at once when searching for available tasks; default is 5.
        /// </summary>
        public int ReadAhead { get; set; }

        /// <summary>
        /// The maximum number of attempts made before failing a task permanently; default is 25.
        /// </summary>
        public int MaximumAttempts { get; set; }

        /// <summary>
        /// The function responsible for calculating the next attempt date after a tasks fails;
        /// default is 5 seconds + N.Pow(4), where N is the number of retries (i.e. exponential backoff)
        /// </summary>
        public Func<int, TimeSpan> IntervalFunction { get; set; }

        /// <summary>
        /// The maximum time each task is allowed before being cancelled; default is 4 hours.
        /// </summary>
        public TimeSpan MaximumRuntime { get; set; }

        /// <summary>
        /// Whether or not failed tasks are deleted from the backend store; default is false.
        /// </summary>
        public bool DeleteOnFailure { get; set; }

        /// <summary>
        /// Whether or not successful tasks are deleted from the backend store; default is false.
        /// </summary>
        public bool DeleteOnSuccess { get; set; }

        /// <summary>
        /// Whether or not tasks that throw exceptions are deleted from the backend store; default is false.
        /// </summary>
        public bool DeleteOnError { get; set; }

        /// <summary>
        /// The default priority level for newly created scheduled tasks that don't specify a priority; default is 0, or highest priority
        /// </summary>
        public int Priority { get; set; }

        public ScheduledProducerSettings()
        {
            // System:
            DelayTasks = true;
            TypeResolver = new DefaultTypeResolver();
            Store = new InMemoryScheduleStore();
            SleepInterval = TimeSpan.FromSeconds(60);
            CleanupInterval = TimeSpan.FromMinutes(5);
            Concurrency = 0;
            ReadAhead = 5;
            MaximumAttempts = 25;
            MaximumRuntime = TimeSpan.FromHours(4);
            IntervalFunction = i => TimeSpan.FromSeconds(5 + (Math.Pow(i, 4)));

            // Per-Task:
            DeleteOnFailure = false;
            DeleteOnSuccess = false;
            DeleteOnError = false;
            Priority = 0;
        }

        /// <summary> Set task values that have defaults if not provided by the user. </summary>
        public void ProvisionTask(ScheduledTask task)
        {
            task.MaximumRuntime = task.MaximumRuntime ?? MaximumRuntime;
            task.MaximumAttempts = task.MaximumAttempts ?? MaximumAttempts;
            task.DeleteOnSuccess = task.DeleteOnSuccess ?? DeleteOnSuccess;
            task.DeleteOnFailure = task.DeleteOnFailure ?? DeleteOnFailure;
            task.DeleteOnError = task.DeleteOnError ?? DeleteOnError;
        }
    }
}