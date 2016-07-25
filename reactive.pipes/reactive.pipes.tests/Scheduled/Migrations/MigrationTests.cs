using System;
using System.Diagnostics;
using System.Linq;
using reactive.tests.Fixtures;
using Xunit;

namespace reactive.tests.Scheduled.Migrations
{
    public class MigrationTests : IClassFixture<SqlServerFixture>
    {
        private readonly SqlServerFixture _db;

        public MigrationTests(SqlServerFixture fixture)
        {
            _db = fixture;
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

            Trace.WriteLine(sql);
        }
    }
}
