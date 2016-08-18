using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using reactive.pipes.Extensions;

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

        public IList<ScheduledTask> GetByAnyTags(params string[] tags)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

                return GetByAnyTagsWithTags(tags, db, t);
            }
        }
        

        public IList<ScheduledTask> GetByAllTags(params string[] tags)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

                return GetByAllTagsWithTags(tags, db, t);
            }
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
                
                UpdateTagMapping(task, db, t);

                t.Commit();
            }
        }

        public void Delete(ScheduledTask task)
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

                const string sql = @"
-- Primary relationship:
DELETE FROM ScheduledTask_Tags WHERE ScheduledTaskId = @Id;
DELETE FROM ScheduledTask WHERE Id = @Id;

-- Remove any orphaned tags:
DELETE FROM ScheduledTask_Tag
WHERE NOT EXISTS (SELECT 1 from ScheduledTask_Tags st WHERE ScheduledTask_Tag.Id = st.TagId)
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

                var tasks = GetUnlockedTasksWithTags(readAhead, db, t);

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

                var task = GetByIdWithTags(id, db, t);

                return task;
            }
        }
        
        public IList<ScheduledTask> GetHangingTasks()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

                var locked = GetLockedTasksWithTags(db, t);

                return locked.Where(st => st.RunningOvertime).ToList();
            }
        }

        public IList<ScheduledTask> GetAll()
        {
            using (var db = new SqlConnection(_connectionString))
            {
                db.Open();

                var t = InTransaction(db);

                return GetAllWithTags(db, t);
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
            task.CreatedAt =
                db.Query<DateTimeOffset>("SELECT CreatedAt FROM ScheduledTask WHERE Id = @Id", task, t).Single();
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
            var user = LockedIdentity.Get();

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

        private static IEnumerable<ScheduledTask> GetLockedTasksWithTags(IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
SELECT ScheduledTask.*, ScheduledTask_Tag.Name FROM ScheduledTask
LEFT JOIN ScheduledTask_Tags ON ScheduledTask_Tags.ScheduledTaskId = ScheduledTask.Id
LEFT JOIN ScheduledTask_Tag ON ScheduledTask_Tags.TagId = ScheduledTask_Tag.Id
WHERE 
    ScheduledTask.LockedAt IS NOT NULL
ORDER BY
    ScheduledTask_Tag.Name ASC    
";
            return QueryWithSplitOnTags(db, t, sql);
        }

        private static List<ScheduledTask> GetUnlockedTasksWithTags(int readAhead, IDbConnection db, IDbTransaction t)
        {
            // None locked, failed or succeeded, must be due, ordered by due time then priority
            const string sql = @"
SELECT TOP {0} st.* FROM ScheduledTask st
WHERE
    st.[LockedAt] IS NULL 
AND
    st.[FailedAt] IS NULL 
AND 
    st.[SucceededAt] IS NULL
AND 
    (st.[RunAt] <= GETUTCDATE())
ORDER BY 
    st.[RunAt], 
    st.[Priority] ASC
";
            var matchSql = string.Format(sql, readAhead);

            var matches = db.Query<ScheduledTask>(matchSql, transaction: t).ToList();

            if (!matches.Any())
                return matches;

            const string fetchSql = @"
SELECT ScheduledTask.*, ScheduledTask_Tag.Name FROM ScheduledTask
LEFT JOIN ScheduledTask_Tags ON ScheduledTask_Tags.ScheduledTaskId = ScheduledTask.Id
LEFT JOIN ScheduledTask_Tag ON ScheduledTask_Tags.TagId = ScheduledTask_Tag.Id
WHERE ScheduledTask.Id IN @Ids
ORDER BY ScheduledTask_Tag.Name ASC
";
            var ids = matches.Select(m => m.Id);

            return QueryWithSplitOnTags(db, t, fetchSql, new { Ids = ids });
        }

        private static ScheduledTask GetByIdWithTags(int id, IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
SELECT ScheduledTask.*, ScheduledTask_Tag.Name FROM ScheduledTask
LEFT JOIN ScheduledTask_Tags ON ScheduledTask_Tags.ScheduledTaskId = ScheduledTask.Id
LEFT JOIN ScheduledTask_Tag ON ScheduledTask_Tags.TagId = ScheduledTask_Tag.Id
WHERE ScheduledTask.Id = @Id
ORDER BY ScheduledTask_Tag.Name ASC
";
            ScheduledTask task = null;
            db.Query<ScheduledTask, string, ScheduledTask>(sql, (s, tag) =>
            {
                task = task ?? s;
                if(tag != null)
                    task.Tags.Add(tag);
                return task;
            }, new { Id = id }, splitOn: "Name", transaction: t);

            return task;
        }

        private static IList<ScheduledTask> GetAllWithTags(IDbConnection db, IDbTransaction t)
        {
            const string sql = @"
SELECT ScheduledTask.*, ScheduledTask_Tag.Name FROM ScheduledTask
LEFT JOIN ScheduledTask_Tags ON ScheduledTask_Tags.ScheduledTaskId = ScheduledTask.Id
LEFT JOIN ScheduledTask_Tag ON ScheduledTask_Tags.TagId = ScheduledTask_Tag.Id
ORDER BY ScheduledTask_Tag.Name ASC
";
            return QueryWithSplitOnTags(db, t, sql);
        }

        private static IList<ScheduledTask> GetByAnyTagsWithTags(string[] tags, SqlConnection db, SqlTransaction t)
        {
            const string matchSql = @"
SELECT st.*
FROM ScheduledTask_Tags stt, ScheduledTask st, ScheduledTask_Tag t
WHERE stt.TagId = t.Id
AND t.Name IN @Tags
AND st.Id = stt.ScheduledTaskId
GROUP BY 
st.[Priority], st.Attempts, st.Handler, st.RunAt, st.MaximumRuntime, st.MaximumAttempts, st.DeleteOnSuccess, st.DeleteOnFailure, st.DeleteOnError, st.Expression, st.Start, st.[End], st.ContinueOnSuccess, st.ContinueOnFailure, st.ContinueOnError, 
st.Id, st.LastError, st.FailedAt, st.SucceededAt, st.LockedAt, st.LockedBy, st.CreatedAt,
t.Name
";
            var matches = db.Query<ScheduledTask>(matchSql, new { Tags = tags }, transaction: t).ToList();

            if (!matches.Any())
                return matches;

            const string fetchSql = @"
SELECT ScheduledTask.*, ScheduledTask_Tag.Name FROM ScheduledTask
LEFT JOIN ScheduledTask_Tags ON ScheduledTask_Tags.ScheduledTaskId = ScheduledTask.Id
LEFT JOIN ScheduledTask_Tag ON ScheduledTask_Tags.TagId = ScheduledTask_Tag.Id
WHERE ScheduledTask.Id IN @Ids
ORDER BY ScheduledTask_Tag.Name ASC
";
            var ids = matches.Select(m => m.Id);

            return QueryWithSplitOnTags(db, t, fetchSql, new { Ids = ids });
        }

        private static IList<ScheduledTask> GetByAllTagsWithTags(string[] tags, IDbConnection db, SqlTransaction t)
        {
            const string matchSql = @"
SELECT st.*
FROM ScheduledTask_Tags stt, ScheduledTask st, ScheduledTask_Tag t
WHERE stt.TagId = t.Id
AND t.Name IN @Tags
AND st.Id = stt.ScheduledTaskId
GROUP BY 
st.[Priority], st.Attempts, st.Handler, st.RunAt, st.MaximumRuntime, st.MaximumAttempts, st.DeleteOnSuccess, st.DeleteOnFailure, st.DeleteOnError, st.Expression, st.Start, st.[End], st.ContinueOnSuccess, st.ContinueOnFailure, st.ContinueOnError, 
st.Id, st.LastError, st.FailedAt, st.SucceededAt, st.LockedAt, st.LockedBy, st.CreatedAt
HAVING COUNT (st.Id) = @Count
";
            var matches = db.Query<ScheduledTask>(matchSql, new { Tags = tags, Count = tags.Length }, transaction: t).ToList();

            if (!matches.Any())
                return matches;

            const string fetchSql = @"
SELECT ScheduledTask.*, ScheduledTask_Tag.Name FROM ScheduledTask
LEFT JOIN ScheduledTask_Tags ON ScheduledTask_Tags.ScheduledTaskId = ScheduledTask.Id
LEFT JOIN ScheduledTask_Tag ON ScheduledTask_Tags.TagId = ScheduledTask_Tag.Id
WHERE ScheduledTask.Id IN @Ids
ORDER BY ScheduledTask_Tag.Name ASC
";
            var ids = matches.Select(m => m.Id);

            return QueryWithSplitOnTags(db, t, fetchSql, new { Ids = ids });
        }

        private static List<ScheduledTask> QueryWithSplitOnTags(IDbConnection db, IDbTransaction t, string sql, object data = null)
        {
            Dictionary<int, ScheduledTask> lookup = new Dictionary<int, ScheduledTask>();
            db.Query<ScheduledTask, string, ScheduledTask>(sql, (s, tag) =>
            {
                ScheduledTask task;
                if (!lookup.TryGetValue(s.Id, out task))
                    lookup.Add(s.Id, task = s);
                if(tag != null)
                    task.Tags.Add(tag);
                return task;
            }, data, splitOn: "Name", transaction: t);

            List<ScheduledTask> result = lookup.Values.ToList();
            return result;
        }

        private static readonly List<string> NoTags = new List<string>();

        private static void UpdateTagMapping(ScheduledTask task, IDbConnection db, IDbTransaction t)
        {
            var source = task.Tags ?? NoTags;

            if (source == NoTags || source.Count == 0)
            {
                db.Execute("DELETE FROM ScheduledTask_Tags WHERE ScheduledTaskId = @Id", task, transaction: t);
                return;
            }

            // normalize for storage
            var normalized = source.Select(st => st.Trim().Replace(" ", "-").Replace("'", "\""));

            // values = VALUES @Tags
            // sql server only permits 1000 values per statement
            foreach (var tags in normalized.Split(1000))
            {
                const string upsertSql = @"
-- upsert tags
MERGE ScheduledTask_Tag
USING (VALUES {0}) AS Pending (Name) 
ON ScheduledTask_Tag.Name = Pending.Name 
WHEN NOT MATCHED THEN
    INSERT (Name) VALUES (Pending.Name)
;

-- sync tag mappings
MERGE ScheduledTask_Tags
USING (SELECT @ScheduledTaskId AS ScheduledTaskId, Id AS TagId FROM ScheduledTask_Tag WHERE Name IN @Tags) AS Pending (ScheduledTaskId, TagId) 
ON ScheduledTask_Tags.ScheduledTaskId = Pending.ScheduledTaskId 
    AND ScheduledTask_Tags.TagId = Pending.TagId 
WHEN NOT MATCHED BY SOURCE THEN
    DELETE
WHEN NOT MATCHED THEN 
    INSERT (ScheduledTaskId,TagId) VALUES (Pending.ScheduledTaskId, Pending.TagId)
;
";
                IEnumerable<string> values = tags.Select(tag => $"('{tag}')");
                string sql = string.Format(upsertSql, string.Join(",", values));
                db.Execute(sql, new { ScheduledTaskId = task.Id, Tags = tags }, transaction: t);
            }
        }
    }
}