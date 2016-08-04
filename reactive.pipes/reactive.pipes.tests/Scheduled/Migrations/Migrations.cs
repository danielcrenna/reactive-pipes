using System;
using FluentMigrator;

namespace reactive.tests.Scheduled.Migrations
{
    [Migration(1)]
    public class Empty : AutoReversingMigration
    {
        public override void Up()
        {
            
        }
    }

    [Migration(2)]
    public class Baseline : Migration
    {
        public override void Up()
        {
            const string schema = "dbo";

            Execute.Sql($"CREATE SEQUENCE [{schema}].[ScheduledTask_Id] START WITH 1 INCREMENT BY 1");

            Create.Table("ScheduledTask")
                .WithColumn("Id").AsCustom($"INT DEFAULT(NEXT VALUE FOR [{schema}].[ScheduledTask_Id]) PRIMARY KEY CLUSTERED")
                .WithColumn("Priority").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Attempts").AsInt32().NotNullable().WithDefaultValue(0)
                .WithColumn("Handler").AsString(int.MaxValue).NotNullable()
                .WithColumn("RunAt").AsDateTimeOffset().NotNullable()
                .WithColumn("MaximumRuntime").AsTime().NotNullable()
                .WithColumn("MaximumAttempts").AsInt32().NotNullable()
                .WithColumn("DeleteOnSuccess").AsBoolean().NotNullable()
                .WithColumn("DeleteOnFailure").AsBoolean().NotNullable()
                .WithColumn("DeleteOnError").AsBoolean().NotNullable()
                .WithColumn("CreatedAt").AsDateTimeOffset().NotNullable().WithDefault(SystemMethods.CurrentUTCDateTime)
                .WithColumn("LastError").AsString().Nullable()
                .WithColumn("FailedAt").AsDateTimeOffset().Nullable()
                .WithColumn("SucceededAt").AsDateTimeOffset().Nullable()
                .WithColumn("LockedAt").AsDateTimeOffset().Nullable()
                .WithColumn("LockedBy").AsString().Nullable()
                .WithColumn("Expression").AsAnsiString().Nullable()
                .WithColumn("Start").AsDateTimeOffset().NotNullable()
                .WithColumn("ContinueOnSuccess").AsBoolean().NotNullable()
                .WithColumn("ContinueOnFailure").AsBoolean().NotNullable()
                .WithColumn("ContinueOnError").AsBoolean().NotNullable()
                .WithColumn("End").AsDateTimeOffset().Nullable()
                ;
        }

        public override void Down()
        {
            Execute.Sql("DROP TABLE [ScheduledTask]");
            Execute.Sql("DROP SEQUENCE [ScheduledTask_Id]");
        }
    }
}
