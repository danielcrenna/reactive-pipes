using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using Dapper;
using Dates;

namespace reactive.pipes.scheduled
{
    public class SqlScheduleStore : IScheduleStore
    {
        private readonly string _connectionString;

        public SqlScheduleStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Save(ScheduledTask task)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = db.BeginTransaction(IsolationLevel.Serializable);

                if (task.Id == 0)
                {
                    InsertScheduledTask(task, db, t);
                }
                else
                {
                    UpdateScheduledTask(task, db, t);
                }

                t.Commit();
            }
        }

        public void Delete(ScheduledTask task)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = db.BeginTransaction(IsolationLevel.Serializable);

                const string sql = @"
DELETE FROM ScheduledTask WHERE Id = @Id; 
DELETE FROM RepeatInfo WHERE ScheduledTaskId = @Id;
";
                db.Execute(sql, task, t);

                t.Commit();
            }
        }

        public IList<ScheduledTask> GetAndLockNextAvailable(int readAhead)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = db.BeginTransaction(IsolationLevel.Serializable);

                // None locked, failed or succeeded, must be due, ordered by due time then priority
                const string sql = @"
SELECT TOP {0} * 
FROM 
    [ScheduledTask] t
WHERE
    [LockedAt] IS NULL 
AND
    [FailedAt] IS NULL 
AND 
    [SucceededAt] IS NULL
AND 
    ([RunAt] IS NULL OR ([RunAt] IS NOT NULL AND GETDATE() >= [RunAt]))
ORDER BY 
    [RunAt], [Priority] ASC
";
                var query = string.Format(sql, readAhead);
                var tasks = db.Query<ScheduledTask>(query, transaction: t).ToList();

                if (tasks.Any())
                {
                    LockTasks(tasks, db, t);

                    foreach (var task in tasks)
                        task.RepeatInfo = GetRepeatInfo(task, db, t);
                }

                t.Commit();
                
                return tasks;
            }
        }

        public ScheduledTask GetById(int id)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = db.BeginTransaction(IsolationLevel.ReadUncommitted);

                const string sql = @"
SELECT * FROM ScheduledTask t
WHERE t.Id = @Id
";
                var task = db.Query<ScheduledTask>(sql, new {Id = id}, t).SingleOrDefault();

                if (task != null)
                    task.RepeatInfo = GetRepeatInfo(task, db, t);

                return task;
            }
        }

        public IList<ScheduledTask> GetAll()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = db.BeginTransaction(IsolationLevel.ReadUncommitted);

                const string sql = @"
SELECT * FROM ScheduledTask t
";
                return db.Query<ScheduledTask>(sql, transaction: t).ToList();
            }
        }

        private static void UpdateScheduledTask(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
UPDATE ScheduledTask 
SET 
    Priority = @Priority, 
    Attempts = @Attempts, 
    Handler = @Handler, 
    RunAt = @RunAt, 
    MaximumRuntime = @MaximumRuntime, 
    MaximumAttempts = @MaximumAttempts, 
    DeleteOnSuccess = @DeleteOnSuccess,
    DeleteOnFailure = @DeleteOnFailure,
    DeleteOnSuccess = @DeleteOnSuccess,
    LastError = @LastError,
    FailedAt = @FailedAt, 
    SucceededAt = @SucceededAt, 
    LockedAt = @LockedAt, 
    LockedBy = @LockedBy
WHERE 
    Id = @Id
";
            db.Execute(sql, task, t);

            if (task.RepeatInfo != null)
            {
                if (GetRepeatInfo(task, db, t) == null)
                {
                    InsertRepeatInfo(task, db, t);
                }
                else
                {
                    UpdateRepeatInfo(task, db, t);
                }
            }
            else
            {
                if (GetRepeatInfo(task, db, t) != null)
                {
                    DeleteRepeatInfo(task, db, t);
                }
            }
        }

        private static void InsertScheduledTask(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
INSERT INTO ScheduledTask
    (Priority, Attempts, Handler, RunAt, MaximumRuntime, MaximumAttempts, DeleteOnSuccess, DeleteOnFailure, DeleteOnError) 
VALUES
    (@Priority, @Attempts, @Handler, @RunAt, @MaximumRuntime, @MaximumAttempts, @DeleteOnSuccess, @DeleteOnFailure, @DeleteOnError);

SELECT SCOPE_IDENTITY() AS [Id];
";
            task.Id = db.Execute(sql, task, t);
            task.CreatedAt = db.Query<DateTimeOffset>("SELECT CreatedAt FROM ScheduledTask WHERE Id = @Id", task, t).Single();

            if (task.RepeatInfo != null)
                InsertRepeatInfo(task, db, t);
        }

        private static void InsertRepeatInfo(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
INSERT INTO RepeatInfo 
    (ScheduledTaskId, PeriodFrequency, PeriodQuantifier, Start, IncludeWeekends, ContinueOnSuccess, ContinueOnFailure, ContinueOnError) 
VALUES
    (@ScheduledTaskId, @PeriodFrequency, @PeriodQuantifier, @Start, @IncludeWeekends, @ContinueOnSuccess, @ContinueOnFailure, @ContinueOnError);
";
            db.Execute(sql, new
            {
                ScheduledTaskId = task.Id,

                task.RepeatInfo.PeriodFrequency,
                task.RepeatInfo.PeriodQuantifier,
                task.RepeatInfo.Start,
                task.RepeatInfo.IncludeWeekends
            }, t);
        }

        private static void UpdateRepeatInfo(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
UPDATE RepeatInfo 
SET
    PeriodFrequency = @PeriodFrequency, 
    PeriodQuantifier = @PeriodQuantifier, 
    Start = @Start, 
    IncludeWeekends = @IncludeWeekends,
    ContinueOnSuccess = @ContinueOnSuccess,
    ContinueOnFailure = @ContinueOnFailure,
    ContinueOnError = @ContinueOnError
WHERE 
    ScheduledJobId = @ScheduledJobId;
";

            db.Execute(sql, new
            {
                ScheduledTaskId = task.Id,

                task.RepeatInfo.PeriodFrequency,
                task.RepeatInfo.PeriodQuantifier,
                task.RepeatInfo.Start,
                task.RepeatInfo.IncludeWeekends,
                task.RepeatInfo.ContinueOnSuccess,
                task.RepeatInfo.ContinueOnFailure,
                task.RepeatInfo.ContinueOnError
            }, t);
        }

        private static void DeleteRepeatInfo(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
DELETE 
FROM RepeatInfo 
WHERE ScheduledTaskId = @Id;
";
            db.Execute(sql, task, t);
        }

        private static RepeatInfo GetRepeatInfo(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
SELECT * 
FROM RepeatInfo 
WHERE ScheduledTaskId = @Id
";
            var result = db.Query(sql, task, t).SingleOrDefault();
            if (result == null)
                return null;

            RepeatInfo repeatInfo = new RepeatInfo(result.Start, new DatePeriod(result.PeriodFrequency, result.PeriodQuantifier));
            return repeatInfo;
        }

        private static void LockTasks(List<ScheduledTask> tasks, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
UPDATE ScheduledTask 
SET 
    LockedAt = @Now, 
    LockedBy = @User 
WHERE Id IN 
    @Ids
";
            var now = DateTime.Now;
            var identity = WindowsIdentity.GetCurrent();
            var user = identity == null ? Environment.UserName : identity.Name;

            db.Execute(sql, new
            {
                Now = now,
                Ids = tasks.Select(task => task.Id),
                User = user
            }, t);

            foreach (var task in tasks)
            {
                task.LockedAt = now;
                task.LockedBy = user;
            }
        }
    }
}