using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace reactive.pipes.Extensions
{
    /// <summary>
    /// See: http://blogs.msdn.com/b/pfxteam/archive/2011/10/24/10229468.aspx
    /// </summary>
    internal static class TaskExtensions
    {
        private static readonly IDictionary<TaskScheduler, TaskFactory> TaskFactories = new ConcurrentDictionary<TaskScheduler, TaskFactory>();

        public static Task Run(this TaskScheduler scheduler, Action action, CancellationToken cancellationToken)
        {
            return WithTaskFactory(scheduler).StartNew(action, cancellationToken);
        }

        public static Task Run<T>(this TaskScheduler scheduler, Func<T> func, CancellationToken cancellationToken)
        {
            return WithTaskFactory(scheduler).StartNew(func, cancellationToken);
        }

        public static Task Run(this TaskScheduler scheduler, Func<Task> func, CancellationToken cancellationToken)
        {
            return WithTaskFactory(scheduler).StartNew(func, cancellationToken).Unwrap();
        }

        public static Task Run<T>(this TaskScheduler scheduler, Func<Task<T>> func, CancellationToken cancellationToken)
        {
            return WithTaskFactory(scheduler).StartNew(func, cancellationToken).Unwrap();
        }

        public static TaskFactory WithTaskFactory(this TaskScheduler scheduler)
        {
            TaskFactory tf;
            if (!TaskFactories.TryGetValue(scheduler, out tf))
                TaskFactories.Add(scheduler, tf = new TaskFactory(CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskContinuationOptions.None, scheduler));
            return tf;
        }
    }
}