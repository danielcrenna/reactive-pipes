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
            var diff = CompareTwoCronOccurences(schedule);
            Assert.Equal(n, diff.Minutes);
        }

        [Theory, InlineData(1)]
        public void Every_n_hours(int n)
        {
            var cron = CronTemplates.Hourly(n);
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurences(schedule);
            Assert.Equal(n, diff.Hours);
        }

        [Theory, InlineData(1), InlineData(5)]
        public void Every_n_days(int n)
        {
            var cron = CronTemplates.Daily(n);
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurences(schedule);
            Assert.Equal(n, diff.Days);
        }

        [Theory, InlineData(DayOfWeek.Sunday)]
        public void Every_nth_weekday(DayOfWeek n)
        {
            var cron = CronTemplates.WeekDaily(n);
            var schedule = CrontabSchedule.Parse(cron);
            var diff = CompareTwoCronOccurences(schedule);
            Assert.Equal(7, diff.Days);
        }

        [Theory]
        [InlineData(DayOfWeek.Monday, DayOfWeek.Tuesday, 1)]
        [InlineData(DayOfWeek.Monday, DayOfWeek.Wednesday, 2)]
        [InlineData(DayOfWeek.Monday, DayOfWeek.Thursday, 3)]
        [InlineData(DayOfWeek.Monday, DayOfWeek.Friday, 4)]
        [InlineData(DayOfWeek.Monday, DayOfWeek.Saturday, 5)]
        [InlineData(DayOfWeek.Monday, DayOfWeek.Sunday, 6)]
        public void Every_nth_and_mth_weekday(DayOfWeek n, DayOfWeek m, int expected)
        {
            string cron = CronTemplates.WeekDaily(onDays: new[] { n, m });
            CrontabSchedule schedule = CrontabSchedule.Parse(cron);

            // These tests would be temporal if we used 'now', so must start from a known fixed date
            DateTime start = new DateTime(2016, 9, 4);
            DateTime from = schedule.GetNextOccurrence(start); // should always start on 9/5/2016 (Monday)
            DateTime to = schedule.GetNextOccurrence(from);
            TimeSpan diff = to - from;
            Assert.Equal(expected, diff.Days);
        }

        [Fact]
        public void Monthly_on_first_of_month()
        {
            string cron = CronTemplates.Monthly();
            CrontabSchedule schedule = CrontabSchedule.Parse(cron);
            TimeSpan diff = CompareTwoCronOccurences(schedule);
            Assert.True(diff.Days == 30 || diff.Days == 31);
        }

        private static TimeSpan CompareTwoCronOccurences(CrontabSchedule schedule)
        {
            DateTime from = schedule.GetNextOccurrence(DateTime.Now); // <-- throw this one away to normalize
            from = schedule.GetNextOccurrence(from);
            DateTime to = schedule.GetNextOccurrence(@from);
            TimeSpan diff = to - @from;
            return diff;
        }
    }
}
