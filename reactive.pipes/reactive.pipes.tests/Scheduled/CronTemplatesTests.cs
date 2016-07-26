using System;
using NCrontab;
using reactive.pipes.scheduled;
using Xunit;

namespace reactive.tests.Scheduled
{
    public class CronTemplatesTests
    {
        [Theory, InlineData(1), InlineData(5)]
        public void Every_n_minutes(int n)
        {
            var cron = CronTemplates.Minutely(n);
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurencs(schedule);
            Assert.Equal(n, diff.Minutes);
        }

        [Theory, InlineData(1), InlineData(5)]
        public void Every_n_hours(int n)
        {
            var cron = CronTemplates.Hourly(n);
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurencs(schedule);
            Assert.Equal(n, diff.Hours);
        }

        [Theory, InlineData(1), InlineData(5)]
        public void Every_n_days(int n)
        {
            var cron = CronTemplates.Daily(n);
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurencs(schedule);
            Assert.Equal(n, diff.Days);
        }

        [Theory, InlineData(DayOfWeek.Monday)]
        public void Every_nth_weekday(DayOfWeek n)
        {
            var cron = CronTemplates.WeekDaily(n);
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurencs(schedule);
            Assert.Equal(7, diff.Days);
        }

        [Theory, InlineData(DayOfWeek.Monday, DayOfWeek.Thursday)]
        public void Every_nth_and_mth_weekday(DayOfWeek n, DayOfWeek m)
        {
            var cron = CronTemplates.WeekDaily(onDays: new []{ n, m });
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurencs(schedule);
            int expected = m - n;
            Assert.Equal(expected, diff.Days);
        }

        [Fact]
        public void Monthly_on_first_of_month()
        {
            var cron = CronTemplates.Monthly();
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurencs(schedule);
            Assert.True(diff.Days == 30 || diff.Days == 31);
        }

        private static TimeSpan CompareTwoCronOccurencs(CrontabSchedule schedule)
        {
            DateTime from = schedule.GetNextOccurrence(DateTime.Now); // <-- throw this one away to normalize
            from = schedule.GetNextOccurrence(from);
            DateTime to = schedule.GetNextOccurrence(@from);
            TimeSpan diff = to - @from;
            return diff;
        }
    }
}
