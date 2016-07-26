using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using Dapper;

namespace reactive.pipes.Scheduler
{
    public class SqlScheduleStore : IScheduleStore
    {
        private readonly string _connectionString;

        public SqlScheduleStore(string connectionString)
        {
            _connectionString = connectionString;
        }

        private static SqlTransaction InTransaction(SqlConnection db)
        {
            return db.BeginTransaction(IsolationLevel.Serializable);
        }

        public void Save(ScheduledTask task)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

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

                var t = InTransaction(db);

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
    ([RunAt] <= GETUTCDATE())
ORDER BY 
    [RunAt], [Priority] ASC
";
                var query = string.Format(sql, readAhead);
                var tasks = db.Query<ScheduledTask>(query, transaction: t).ToList();

                if (tasks.Any())
                    LockTasks(tasks, db, t);

                t.Commit();

                return tasks;
            }
        }

        public ScheduledTask GetById(int id)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

                const string sql = @"
SELECT * FROM ScheduledTask t
WHERE t.Id = @Id
";
                var task = db.Query<ScheduledTask>(sql, new {Id = id}, t).SingleOrDefault();

                return task;
            }
        }

        public IList<ScheduledTask> GetAll()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

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
    DeleteOnError = @DeleteOnError,
    Expression = @Expression, 
    Start = @Start, 
    [End] = @End,
    ContinueOnSuccess = @ContinueOnSuccess,
    ContinueOnFailure = @ContinueOnFailure,
    ContinueOnError = @ContinueOnError,
    LastError = @LastError,
    FailedAt = @FailedAt, 
    SucceededAt = @SucceededAt, 
    LockedAt = @LockedAt, 
    LockedBy = @LockedBy
WHERE 
    Id = @Id
";
            db.Execute(sql, task, t);
        }

        private static void InsertScheduledTask(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
INSERT INTO ScheduledTask
    (Priority, Attempts, Handler, RunAt, MaximumRuntime, MaximumAttempts, DeleteOnSuccess, DeleteOnFailure, DeleteOnError, Expression, Start, [End], ContinueOnSuccess, ContinueOnFailure, ContinueOnError) 
VALUES
    (@Priority, @Attempts, @Handler, @RunAt, @MaximumRuntime, @MaximumAttempts, @DeleteOnSuccess, @DeleteOnFailure, @DeleteOnError, @Expression, @Start, @End, @ContinueOnSuccess, @ContinueOnFailure, @ContinueOnError);

SELECT MAX(Id) FROM [ScheduledTask];
";
            task.Id = db.Query<int>(sql, task, t).Single();
            task.CreatedAt = db.Query<DateTimeOffset>("SELECT CreatedAt FROM ScheduledTask WHERE Id = @Id", task, t).Single();
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