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
    public class Baseline : AutoReversingMigration
    {
        public override void Up()
        {
            Create.Table("ScheduledTask")
                .WithColumn("Id").AsInt32().Identity().PrimaryKey()
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
                ;

            Create.Table("RepeatInfo")
                .WithColumn("ScheduledTaskId").AsInt32().Identity().ForeignKey("ScheduledTask", "Id")
                .WithColumn("PeriodFrequency").AsInt32().NotNullable()
                .WithColumn("PeriodQuantifier").AsInt32().NotNullable()
                .WithColumn("Start").AsDateTimeOffset().NotNullable()
                .WithColumn("IncludeWeekends").AsBoolean().NotNullable().WithDefaultValue(false)
                ;
        }
    }
}
