using System.Collections.Generic;

namespace reactive.pipes.Scheduler
{
    public interface IScheduleStore
    {
        void Save(ScheduledTask task);
        void Delete(ScheduledTask task);
        IList<ScheduledTask> GetAll();
        IList<ScheduledTask> GetAndLockNextAvailable(int readAhead);
        ScheduledTask GetById(int id);
    }
}