using System.Collections.Generic;

namespace reactive.pipes.Scheduler
{
    public interface IScheduleStore
    {
        IList<ScheduledTask> GetAll();
        IList<ScheduledTask> GetHangingTasks();
        ScheduledTask GetById(int id);

        void Save(ScheduledTask task);
        void Delete(ScheduledTask task);
        IList<ScheduledTask> GetAndLockNextAvailable(int readAhead);
    }
}