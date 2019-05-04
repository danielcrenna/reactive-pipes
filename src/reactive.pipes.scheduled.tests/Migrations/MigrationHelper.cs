// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Announcers;
using FluentMigrator.Runner.Initialization;

namespace reactive.pipes.scheduled.tests.Migrations
{
	internal class MigrationHelper
	{
		public static string MigrateToLatest(string databaseType, string connectionString, string profile = null,
			Assembly assembly = null, bool trace = false, IEnumerable<string> tags = null, bool sqlOnly = false)
		{
			return MigrateToVersion(databaseType, connectionString, 0, profile, assembly, trace, tags, sqlOnly);
		}

		public static string MigrateToVersion(string databaseType, string connectionString, long version,
			string profile = null, Assembly assembly = null, bool trace = false, IEnumerable<string> tags = null,
			bool sqlOnly = false)
		{
			return ExecuteMigration(databaseType, connectionString, version, profile, assembly, trace, tags, sqlOnly);
		}

		private static string ExecuteMigration(string databaseType, string connectionString, long version,
			string profile, Assembly assembly, bool trace, IEnumerable<string> tags, bool sqlOnly = false)
		{
			assembly = assembly ?? Assembly.GetExecutingAssembly();

			var outputPath = Path.GetTempFileName();

			using (var sw = new StreamWriter(outputPath))
			{
				var announcers = new List<IAnnouncer>();
				if (sqlOnly)
					announcers.Add(new TextWriterAnnouncer(sw) {ShowSql = true});
				if (trace)
					announcers.Add(new ConsoleAnnouncer {ShowSql = true});
				if (announcers.Count == 0)
					announcers.Add(new NullAnnouncer());
				var context = new RunnerContext(new CompositeAnnouncer(announcers.ToArray()))
				{
					Connection = connectionString,
					Database = databaseType,
					Targets = new[] {assembly.FullName},
					Version = version,
					Profile = profile,
					Timeout = 0,
					Tags = tags,
					PreviewOnly = sqlOnly
				};
				var executor = new TaskExecutor(context);
				executor.Execute();
			}

			if (string.IsNullOrWhiteSpace(outputPath))
			{
				File.Delete(outputPath);
				return string.Empty;
			}

			var sql = File.ReadAllText(outputPath);
			File.Delete(outputPath);
			return sql;
		}
	}
}