using reactive.pipes.scheduled;
using reactive.tests.Fixtures;
using reactive.tests.Scheduled.Migrations;
using Xunit;

namespace reactive.tests.Scheduled
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
