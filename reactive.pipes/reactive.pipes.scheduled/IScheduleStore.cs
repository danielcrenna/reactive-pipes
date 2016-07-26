using System.Collections.Generic;

namespace reactive.pipes.scheduled
{
    public interface IScheduleStore
    {
        void Save(ScheduledTask task);
        void Delete(ScheduledTask task);
        IList<ScheduledTask> GetAndLockNextAvailable(int readAhead);
        ScheduledTask GetById(int id);
        IList<ScheduledTask> GetAll();
    }
}