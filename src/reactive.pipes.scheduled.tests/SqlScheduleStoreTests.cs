// Copyright (c) Daniel Crenna. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using reactive.pipes.scheduled.tests.Fixtures;
using reactive.pipes.scheduled.tests.Migrations;
using Xunit;

namespace reactive.pipes.scheduled.tests
{
	public class SqlScheduleStoreTests : ScheduleStoreTests, IClassFixture<SqlServerFixture>
	{
		public SqlScheduleStoreTests(SqlServerFixture db)
		{
			MigrationHelper.MigrateToLatest("sqlserver", db.ConnectionString);

			Store = new SqlScheduleStore(db.ConnectionString);
		}
	}
}