// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using Dapper;
using reactive.pipes.Extensions;

namespace reactive.pipes.scheduled
{
	public class SqlScheduleStore : IScheduleStore
	{
		private static readonly List<string> NoTags = new List<string>();
		private readonly string _connectionString;
		private readonly string _schema;
		private readonly string _tablePrefix;


		public SqlScheduleStore(string connectionString, string schema = "dbo", string tablePrefix = "ScheduledTask")
		{
			_connectionString = connectionString;
			_schema = schema;
			_tablePrefix = tablePrefix;
		}

		internal string TaskTable => $"[{_schema}].[{_tablePrefix}]";
		internal string TagTable => $"[{_schema}].[{_tablePrefix}_Tag]";
		internal string TagsTable => $"[{_schema}].[{_tablePrefix}_Tags]";

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
					InsertScheduledTask(task, db, t);
				else
					UpdateScheduledTask(task, db, t);

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

				var sql = $@"
-- Primary relationship:
DELETE FROM {TagsTable} WHERE ScheduledTaskId = @Id;
DELETE FROM {TaskTable} WHERE Id = @Id;

-- Remove any orphaned tags:
DELETE FROM {TagTable}
WHERE NOT EXISTS (SELECT 1 FROM {TagsTable} st WHERE {TagTable}.Id = st.TagId)
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

		private static SqlTransaction InTransaction(SqlConnection db)
		{
			return db.BeginTransaction(IsolationLevel.Serializable);
		}

		private void UpdateScheduledTask(ScheduledTask task, IDbConnection db, IDbTransaction t)
		{
			var sql = $@"
UPDATE {TaskTable} 
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

		private void InsertScheduledTask(ScheduledTask task, IDbConnection db, IDbTransaction t)
		{
			var sql = $@"
INSERT INTO {TaskTable} 
    (Priority, Attempts, Handler, RunAt, MaximumRuntime, MaximumAttempts, DeleteOnSuccess, DeleteOnFailure, DeleteOnError, Expression, Start, [End], ContinueOnSuccess, ContinueOnFailure, ContinueOnError) 
VALUES
    (@Priority, @Attempts, @Handler, @RunAt, @MaximumRuntime, @MaximumAttempts, @DeleteOnSuccess, @DeleteOnFailure, @DeleteOnError, @Expression, @Start, @End, @ContinueOnSuccess, @ContinueOnFailure, @ContinueOnError);

SELECT MAX(Id) FROM {TaskTable};
";
			task.Id = db.Query<int>(sql, task, t).Single();
			task.CreatedAt = db.Query<DateTimeOffset>($"SELECT CreatedAt FROM {TaskTable} WHERE Id = @Id", task, t)
				.Single();
		}


		private void LockTasks(IReadOnlyCollection<ScheduledTask> tasks, IDbConnection db, IDbTransaction t)
		{
			var sql = $@"
UPDATE {TaskTable}  
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

		private IEnumerable<ScheduledTask> GetLockedTasksWithTags(IDbConnection db, IDbTransaction t)
		{
			var sql = $@"
SELECT {TaskTable}.*, {TagTable}.Name FROM {TaskTable}
LEFT JOIN {TagsTable} ON {TagsTable}.ScheduledTaskId = {TaskTable}.Id
LEFT JOIN {TagTable} ON {TagsTable}.TagId = {TagTable}.Id
WHERE 
    {TaskTable}.LockedAt IS NOT NULL
ORDER BY
    {TagTable}.Name ASC    
";
			return QueryWithSplitOnTags(db, t, sql);
		}

		private List<ScheduledTask> GetUnlockedTasksWithTags(int readAhead, IDbConnection db, IDbTransaction t)
		{
			// None locked, failed or succeeded, must be due, ordered by due time then priority
			var sql = $@"
SELECT TOP {{0}} st.* FROM {TaskTable} st
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

			var fetchSql = $@"
SELECT {TaskTable}.*, {TagTable}.Name FROM {TaskTable}
LEFT JOIN {TagsTable} ON {TagsTable}.ScheduledTaskId = {TaskTable}.Id
LEFT JOIN {TagTable} ON {TagsTable}.TagId = {TagTable}.Id
WHERE {TaskTable}.Id IN @Ids
ORDER BY {TagTable}.Name ASC
";
			var ids = matches.Select(m => m.Id);

			return QueryWithSplitOnTags(db, t, fetchSql, new {Ids = ids});
		}

		private ScheduledTask GetByIdWithTags(int id, IDbConnection db, IDbTransaction t)
		{
			var sql = $@"
SELECT {TaskTable}.*, {TagTable}.Name FROM {TaskTable}
LEFT JOIN {TagsTable} ON {TagsTable}.ScheduledTaskId = {TaskTable}.Id
LEFT JOIN {TagTable} ON {TagsTable}.TagId = {TagTable}.Id
WHERE {TaskTable}.Id = @Id
ORDER BY {TagTable}.Name ASC
";
			ScheduledTask task = null;
			db.Query<ScheduledTask, string, ScheduledTask>(sql, (s, tag) =>
			{
				task = task ?? s;
				if (tag != null)
					task.Tags.Add(tag);
				return task;
			}, new {Id = id}, splitOn: "Name", transaction: t);

			return task;
		}

		private IList<ScheduledTask> GetAllWithTags(IDbConnection db, IDbTransaction t)
		{
			var sql = $@"
SELECT {TaskTable}.*, {TagTable}.Name FROM {TaskTable}
LEFT JOIN {TagsTable} ON {TagsTable}.ScheduledTaskId = {TaskTable}.Id
LEFT JOIN {TagTable} ON {TagsTable}.TagId = {TagTable}.Id
ORDER BY {TagTable}.Name ASC
";
			return QueryWithSplitOnTags(db, t, sql);
		}

		private IList<ScheduledTask> GetByAnyTagsWithTags(string[] tags, SqlConnection db, SqlTransaction t)
		{
			var matchSql = $@"
SELECT st.*
FROM {TagsTable} stt, {TaskTable} st, {TagTable} t
WHERE stt.TagId = t.Id
AND t.Name IN @Tags
AND st.Id = stt.ScheduledTaskId
GROUP BY 
st.[Priority], st.Attempts, st.Handler, st.RunAt, st.MaximumRuntime, st.MaximumAttempts, st.DeleteOnSuccess, st.DeleteOnFailure, st.DeleteOnError, st.Expression, st.Start, st.[End], st.ContinueOnSuccess, st.ContinueOnFailure, st.ContinueOnError, 
st.Id, st.LastError, st.FailedAt, st.SucceededAt, st.LockedAt, st.LockedBy, st.CreatedAt,
t.Name
";
			var matches = db.Query<ScheduledTask>(matchSql, new {Tags = tags}, t).ToList();

			if (!matches.Any())
				return matches;

			var fetchSql = $@"
SELECT {TaskTable}.*, {TagTable}.Name FROM {TaskTable}
LEFT JOIN {TagsTable} ON {TagsTable}.ScheduledTaskId = {TaskTable}.Id
LEFT JOIN {TagTable} ON {TagsTable}.TagId = {TagTable}.Id
WHERE {TaskTable}.Id IN @Ids
ORDER BY {TagTable}.Name ASC
";
			var ids = matches.Select(m => m.Id);

			return QueryWithSplitOnTags(db, t, fetchSql, new {Ids = ids});
		}

		private IList<ScheduledTask> GetByAllTagsWithTags(string[] tags, IDbConnection db, SqlTransaction t)
		{
			var matchSql = $@"
SELECT st.*
FROM {TagsTable} stt, {TaskTable} st, {TagTable} t
WHERE stt.TagId = t.Id
AND t.Name IN @Tags
AND st.Id = stt.ScheduledTaskId
GROUP BY 
st.[Priority], st.Attempts, st.Handler, st.RunAt, st.MaximumRuntime, st.MaximumAttempts, st.DeleteOnSuccess, st.DeleteOnFailure, st.DeleteOnError, st.Expression, st.Start, st.[End], st.ContinueOnSuccess, st.ContinueOnFailure, st.ContinueOnError, 
st.Id, st.LastError, st.FailedAt, st.SucceededAt, st.LockedAt, st.LockedBy, st.CreatedAt
HAVING COUNT (st.Id) = @Count
";
			var matches = db.Query<ScheduledTask>(matchSql, new {Tags = tags, Count = tags.Length}, t).ToList();

			if (!matches.Any())
				return matches;

			var fetchSql = $@"
SELECT {TaskTable}.*, {TagTable}.Name FROM {TaskTable}
LEFT JOIN {TagsTable} ON {TagsTable}.ScheduledTaskId = {TaskTable}.Id
LEFT JOIN {TagTable} ON {TagsTable}.TagId = {TagTable}.Id
WHERE {TaskTable}.Id IN @Ids
ORDER BY {TagTable}.Name ASC
";
			var ids = matches.Select(m => m.Id);

			return QueryWithSplitOnTags(db, t, fetchSql, new {Ids = ids});
		}

		private static List<ScheduledTask> QueryWithSplitOnTags(IDbConnection db, IDbTransaction t, string sql,
			object data = null)
		{
			var lookup = new Dictionary<int, ScheduledTask>();
			db.Query<ScheduledTask, string, ScheduledTask>(sql, (s, tag) =>
			{
				ScheduledTask task;
				if (!lookup.TryGetValue(s.Id, out task))
					lookup.Add(s.Id, task = s);
				if (tag != null)
					task.Tags.Add(tag);
				return task;
			}, data, splitOn: "Name", transaction: t);

			var result = lookup.Values.ToList();
			return result;
		}

		private void UpdateTagMapping(ScheduledTask task, IDbConnection db, IDbTransaction t)
		{
			var source = task.Tags ?? NoTags;

			if (source == NoTags || source.Count == 0)
			{
				db.Execute($"DELETE FROM {TagsTable} WHERE ScheduledTaskId = @Id", task, t);
				return;
			}

			// normalize for storage
			var normalized = source.Select(st => st.Trim().Replace(" ", "-").Replace("'", "\""));

			// values = VALUES @Tags
			// sql server only permits 1000 values per statement
			foreach (var tags in normalized.Split(1000))
			{
				var upsertSql = $@"
-- upsert tags
MERGE {TagTable}
USING (VALUES {{0}}) AS Pending (Name) 
ON {TagTable}.Name = Pending.Name 
WHEN NOT MATCHED THEN
    INSERT (Name) VALUES (Pending.Name)
;

-- sync tag mappings
MERGE {TagsTable}
USING
    (SELECT @ScheduledTaskId AS ScheduledTaskId, Id AS TagId 
       FROM {TagTable} 
      WHERE Name IN @Tags) AS Pending (ScheduledTaskId, TagId) 
ON {TagsTable}.ScheduledTaskId = Pending.ScheduledTaskId 
AND {TagsTable}.TagId = Pending.TagId 
WHEN NOT MATCHED BY SOURCE AND {TagsTable}.ScheduledTaskId = @ScheduledTaskId THEN
    DELETE 
WHEN NOT MATCHED THEN 
    INSERT (ScheduledTaskId,TagId) VALUES (Pending.ScheduledTaskId, Pending.TagId)
;
";
				var values = tags.Select(tag => $"('{tag}')");
				var sql = string.Format(upsertSql, string.Join(",", values));
				db.Execute(sql, new {ScheduledTaskId = task.Id, Tags = tags}, t);
			}
		}
	}
}