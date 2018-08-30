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
