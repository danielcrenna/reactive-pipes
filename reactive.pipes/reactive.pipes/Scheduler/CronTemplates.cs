using System;

namespace reactive.pipes.Scheduler
{
    public class CronTemplates
    {
        public static string Minutely(int minutes = 0)
        {
            return $"{ValueOrStar(minutes)} * * * *";
        }

        public static string Hourly(int hours = 0, int atMinute = 0)
        {
            return $"{atMinute} {ValueOrStar(hours)} * * *";
        }

        public static string Daily(int days, int atHour = 0, int atMinute = 0)
        {
            return $"{atMinute} {atHour} */{days} * *";
        }

        public static string WeekDaily(DayOfWeek onDay, int atHour = 0, int atMinute = 0)
        {
            return $"{atMinute} {atHour} * * {(int)onDay + 1}";
        }

        public static string WeekDaily(int atHour = 0, int atMinute = 0, params DayOfWeek[] onDays)
        {
            if (onDays.Length == 0)
                return null;

            var expression = WeekDaily(onDays[0], atHour, atMinute);

            for (var i = 1; i < onDays.Length; i++)
            {
                expression = $"{expression},{(int)onDays[i] + 1}";
            }

            return expression;
        }

        public static string Monthly(int onDay = 1, int atHour = 0, int atMinute = 0)
        {
            return $"{atMinute} {atHour} {onDay} * *";
        }

        private static string ValueOrStar(int minutes)
        {
            return minutes == 0 ? "*" : "*/" + minutes;
        }
    }
}