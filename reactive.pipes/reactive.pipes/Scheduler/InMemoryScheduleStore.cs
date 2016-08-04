using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace reactive.pipes.Scheduler
{
    public class InMemoryScheduleStore : IScheduleStore
    {
        private static int _identity;

        private readonly IDictionary<int, HashSet<ScheduledTask>>  _tasks;

        public InMemoryScheduleStore()
        {
            _tasks = new ConcurrentDictionary<int, HashSet<ScheduledTask>>();
        }

        public IList<ScheduledTask> GetByAnyTags(params string[] tags)
        {
            var all = GetAll();

            var query = all.Where(a =>
            {
                return tags.Any(tag => a.Tags.Contains(tag));
            });

            return query.ToList();
        }

        public IList<ScheduledTask> GetByAllTags(params string[] tags)
        {
            var all = GetAll();

            var query = all.Where(a =>
            {
                return tags.All(tag => a.Tags.Contains(tag));
            });

            return query.ToList();

        }

        public void Save(ScheduledTask task)
        {
            HashSet<ScheduledTask> tasks;
            if(!_tasks.TryGetValue(task.Priority, out tasks))
                _tasks.Add(task.Priority, tasks = new HashSet<ScheduledTask>());

            if (tasks.All(t => t.Id != task.Id))
            {
                tasks.Add(task);
                task.Id = ++_identity;
            }
        }

        public void Delete(ScheduledTask task)
        {
            HashSet<ScheduledTask> tasks;
            if (_tasks.TryGetValue(task.Priority, out tasks))
                tasks.Remove(task);
        }
       
        public IList<ScheduledTask> GetAndLockNextAvailable(int readAhead)
        {
            var all = _tasks.SelectMany(t => t.Value);

            // None locked, failed or succeeded, must be due, ordered by due time then priority
            var now = DateTimeOffset.UtcNow;

            var query = all
                .Where(t => !t.FailedAt.HasValue && !t.SucceededAt.HasValue && !t.LockedAt.HasValue)
                .Where(t => t.RunAt <= now)
                .OrderBy(t => t.RunAt)
                .ThenBy(t => t.Priority);

            var tasks = query.Count() > readAhead ? query.Take(readAhead).ToList() : query.ToList();

            // Lock tasks:
            if (tasks.Any())
            {
                foreach (ScheduledTask scheduledTask in tasks)
                {
                    scheduledTask.LockedAt = now;
                    scheduledTask.LockedBy = LockedIdentity.Get();
                }
            }

            return tasks;
        }

        public ScheduledTask GetById(int id)
        {
            return _tasks.SelectMany(t => t.Value).SingleOrDefault(t => t.Id == id);
        }

        public IList<ScheduledTask> GetHangingTasks()
        {
            return GetAll().Where(t => t.RunningOvertime).ToList();
        }

        public IList<ScheduledTask> GetAll()
        {
            IEnumerable<ScheduledTask> all = _tasks.SelectMany(t => t.Value).OrderBy(t => t.Priority);

            return all.ToList();
        }
    }
}