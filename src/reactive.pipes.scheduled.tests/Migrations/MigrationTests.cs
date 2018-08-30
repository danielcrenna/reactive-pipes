using System;
using System.Linq;
using reactive.pipes.scheduled.tests.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace reactive.pipes.scheduled.tests.Migrations
{
    public class MigrationTests : IClassFixture<SqlServerFixture>
    {
        private readonly SqlServerFixture _db;
	    private readonly ITestOutputHelper _console;

	    public MigrationTests(SqlServerFixture fixture, ITestOutputHelper console)
	    {
		    _db = fixture;
		    _console = console;
	    }

        [Fact]
        public void Migrates_to_latest_version()
        {
            MigrationHelper.MigrateToLatest("sqlserver", _db.ConnectionString);
        }

        [Fact]
        public void Produces_migration_script()
        {
            const int start = 1;
            const int end = 2;

            MigrationHelper.MigrateToVersion("sqlserver", _db.ConnectionString, start);
            
            var sql = MigrationHelper.MigrateToVersion("sqlserver", _db.ConnectionString, end, sqlOnly: true, trace: true);
            Assert.NotNull(sql);

            string[] lines = sql.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            lines = lines.Where(l => !l.StartsWith("INSERT INTO [dbo].[VersionInfo]")).ToArray();
            sql = string.Join(Environment.NewLine, lines);

	        _console.WriteLine(sql);
        }
    }
}
