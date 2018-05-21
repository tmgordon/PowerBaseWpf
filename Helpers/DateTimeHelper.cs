using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;

namespace PowerBaseWpf.Helpers
{
    public static class DateTimeHelper
    {

        public static string ToDisplay(this TimeSpan timespan)
        {
            return $"{timespan:hh\\:mm\\:ss}";
        }


        public static bool WithinTimeSpan(this DateTime dateOne, TimeSpan timeSpan)
        {
            var now = DateTime.Now;
            return dateOne.WithinRange(new Tuple<DateTime, DateTime>(now.Subtract(timeSpan), now));
        }
        public static bool WithinDay(this DateTime? nDateOne, DateTime? nDateTwo = null)
        {
            var dateOne = nDateOne ?? DateTime.Now;
            var dateTwo = nDateTwo ?? DateTime.Now;

            var range = dateTwo.GetDayRange();
            return dateOne.WithinRange(range);
        }
        public static bool WithinDay(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            var range = dateTwo.GetDayRange();
            return dateOne.WithinRange(range);
        }
        public static bool WithinWeek(this DateTime? nDateOne, DateTime? nDateTwo = null)
        {
            var dateOne = nDateOne ?? DateTime.Now;
            var dateTwo = nDateTwo ?? DateTime.Now;
            var range = dateTwo.GetWeekRange();
            return dateOne.WithinRange(range);
        }
        public static bool WithinWeek(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            var range = dateTwo.GetWeekRange();
            return dateOne.WithinRange(range);
        }
        public static bool WithinMonth(this DateTime? nDateOne, DateTime? nDateTwo = null)
        {
            var dateOne = nDateOne ?? DateTime.Now;
            var dateTwo = nDateTwo ?? DateTime.Now;
            var range = dateTwo.GetMonthRange();
            return dateOne.WithinRange(range);
        }
        public static bool WithinMonth(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            var range = dateTwo.GetMonthRange();
            return dateOne.WithinRange(range);
        }
        public static bool WithinRange(this DateTime? nDateOne, Tuple<DateTime, DateTime> dateRange)
        {
            var dateOne = nDateOne ?? DateTime.Now;
            return dateRange.Item1 <= dateOne && dateOne <= dateRange.Item2;
        }
        public static bool WithinRange(this DateTime dateOne, Tuple<DateTime, DateTime> dateRange)
        {
            return dateRange.Item1 <= dateOne && dateOne <= dateRange.Item2;
        }
        
        public static Tuple<DateTime, DateTime> GetDayRange(this DateTime dateOne)
        {
            dateOne = dateOne.Date;
            var start = dateOne;
            var end = dateOne.AddDays(1).AddMilliseconds(-1);
            return new Tuple<DateTime, DateTime>(start, end);
        }

        public static Tuple<DateTime, DateTime> GetMonthRange(this DateTime dateOne)
        {
            dateOne = dateOne.Date;
            var start = new DateTime(dateOne.Year, dateOne.Month, 1);
            var end = new DateTime(dateOne.Year, dateOne.Month, DateTime.DaysInMonth(dateOne.Year, dateOne.Month)).AddDays(1).AddMilliseconds(-1);
            return new Tuple<DateTime, DateTime>(start, end);
        }

        public static Tuple<DateTime, DateTime> GetWeekRange(this DateTime dateOne)
        {
            dateOne = dateOne.Date;
            DateTime start = dateOne.AddDays(-(int) dateOne.DayOfWeek);
            DateTime end = dateOne.AddDays(6 - (int) dateOne.DayOfWeek).AddDays(1).AddMilliseconds(-1);
            return new Tuple<DateTime, DateTime>(start, end);
        }

        public static TimeSpan DateDifference(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            TimeSpan timespan = dateOne.Year >= dateTwo.Year ? dateOne.Subtract(dateTwo) : dateTwo.Subtract(dateOne);
            return timespan;
        }
        public static double DayDifference(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            TimeSpan timespan = dateOne.Year >= dateTwo.Year ? dateOne.Subtract(dateTwo) : dateTwo.Subtract(dateOne);
            return timespan.TotalDays;
        }

        public static double HourDifference(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            TimeSpan timespan = dateOne.Year >= dateTwo.Year ? dateOne.Subtract(dateTwo) : dateTwo.Subtract(dateOne);
            return timespan.TotalHours;
        }

        public static double MinuteDifference(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            TimeSpan timespan = dateOne.Year >= dateTwo.Year ? dateOne.Subtract(dateTwo) : dateTwo.Subtract(dateOne);
            return timespan.TotalMinutes;
        }
        public static double SecondDifference(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            TimeSpan timespan = dateOne.Year >= dateTwo.Year ? dateOne.Subtract(dateTwo) : dateTwo.Subtract(dateOne);
            return timespan.TotalSeconds;
        }
        public static double MillisecondDifference(this DateTime dateOne, DateTime? nDateTwo = null)
        {
            var dateTwo = nDateTwo ?? DateTime.Now;
            TimeSpan timespan = dateOne.Year >= dateTwo.Year ? dateOne.Subtract(dateTwo) : dateTwo.Subtract(dateOne);
            return timespan.TotalMilliseconds;
        }
    }
}
