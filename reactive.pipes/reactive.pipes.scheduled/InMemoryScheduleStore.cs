using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;

namespace reactive.pipes.scheduled
{
    public class InMemoryScheduleStore : IScheduleStore
    {
        private static int identity = 0;
        private readonly IDictionary<int, HashSet<ScheduledTask>>  _tasks;

        public InMemoryScheduleStore()
        {
            _tasks = new ConcurrentDictionary<int, HashSet<ScheduledTask>>();
        }

        public void Save(ScheduledTask task)
        {
            HashSet<ScheduledTask> tasks;
            if(!_tasks.TryGetValue(task.Priority, out tasks))
                _tasks.Add(task.Priority, tasks = new HashSet<ScheduledTask>());
            tasks.Add(task);
            task.Id = ++identity;
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

            // None failed or succeeded, none locked, RunAt sorted, Priority sorted:
            var query = all
                .Where(t => !t.FailedAt.HasValue && !t.SucceededAt.HasValue && !t.LockedAt.HasValue)
                .OrderBy(t => t.RunAt)
                .ThenBy(t => t.Priority);

            var tasks = query.Count() > readAhead ? query.Take(readAhead).ToList() : query.ToList();

            // Lock tasks:
            var now = DateTimeOffset.UtcNow;
            foreach (ScheduledTask scheduledTask in tasks)
            {
                scheduledTask.LockedAt = now;

                var user = WindowsIdentity.GetCurrent();
                scheduledTask.LockedBy = user == null ? Environment.UserName : user.Name;
            }

            return tasks;
        }

        public ScheduledTask GetById(int id)
        {
            return _tasks.SelectMany(t => t.Value).SingleOrDefault(t => t.Id == id);
        }

        public IList<ScheduledTask> GetAll()
        {
            IEnumerable<ScheduledTask> all = _tasks.SelectMany(t => t.Value).OrderBy(t => t.Priority);

            return all.ToList();
        }
    }
}