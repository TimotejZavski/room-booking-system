using System;
using System.Collections.Generic;

namespace WebApplication1.Models
{
    public class SchoolPeriod
    {
        public int PeriodNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }

        public static readonly List<SchoolPeriod> Periods = new List<SchoolPeriod>
        {
            new SchoolPeriod { PeriodNumber = 1, StartTime = new TimeSpan(7, 10, 0), EndTime = new TimeSpan(7, 55, 0) },
            new SchoolPeriod { PeriodNumber = 2, StartTime = new TimeSpan(8, 0, 0), EndTime = new TimeSpan(8, 45, 0) },
            new SchoolPeriod { PeriodNumber = 3, StartTime = new TimeSpan(8, 50, 0), EndTime = new TimeSpan(9, 35, 0) },
            new SchoolPeriod { PeriodNumber = 4, StartTime = new TimeSpan(9, 40, 0), EndTime = new TimeSpan(10, 25, 0) },
            new SchoolPeriod { PeriodNumber = 5, StartTime = new TimeSpan(10, 30, 0), EndTime = new TimeSpan(11, 15, 0) },
            new SchoolPeriod { PeriodNumber = 6, StartTime = new TimeSpan(11, 20, 0), EndTime = new TimeSpan(12, 5, 0) },
            new SchoolPeriod { PeriodNumber = 7, StartTime = new TimeSpan(12, 10, 0), EndTime = new TimeSpan(12, 55, 0) },
            new SchoolPeriod { PeriodNumber = 8, StartTime = new TimeSpan(13, 0, 0), EndTime = new TimeSpan(13, 45, 0) },
            new SchoolPeriod { PeriodNumber = 9, StartTime = new TimeSpan(13, 50, 0), EndTime = new TimeSpan(14, 35, 0) }
        };

        public static List<SchoolPeriod> GetMissedPeriods(DateTime startTime, DateTime endTime)
        {
            var result = new List<SchoolPeriod>();
            var startTimeOfDay = startTime.TimeOfDay;
            var endTimeOfDay = endTime.TimeOfDay;

            foreach (var period in Periods)
            {
                // Check if this period overlaps with the absence time
                if ((startTimeOfDay <= period.EndTime && endTimeOfDay >= period.StartTime) ||
                    (startTimeOfDay >= period.StartTime && startTimeOfDay < period.EndTime) ||
                    (endTimeOfDay > period.StartTime && endTimeOfDay <= period.EndTime))
                {
                    result.Add(period);
                }
            }

            return result;
        }
        
        public static string GetDayOfWeekSlovene(DayOfWeek dayOfWeek)
        {
            return dayOfWeek switch
            {
                DayOfWeek.Monday => "ponedeljek",
                DayOfWeek.Tuesday => "torek",
                DayOfWeek.Wednesday => "sreda",
                DayOfWeek.Thursday => "četrtek",
                DayOfWeek.Friday => "petek",
                DayOfWeek.Saturday => "sobota",
                DayOfWeek.Sunday => "nedelja",
                _ => dayOfWeek.ToString()
            };
        }
    }
} 