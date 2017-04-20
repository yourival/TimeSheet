using System;
using TimeSheet.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace TimeSheet.Tests.Model
{
    [TestClass]
    public class PayPeriodTest
    {
        [TestMethod]
        [Timeout(30000)]
        public void TestGetHoliday()
        {
            // arrange
            List<TimeRecord> period = new List<TimeRecord>();
            DateTime firstDate = new DateTime(2016, 12, 14);
            for (int i = 0; i < 14; i++)
            {
                period.Add(new TimeRecord(firstDate.AddDays(i)));
            }

            // Act
            List<DateTime> holidays = PayPeriod.GetHoliday();
            foreach(TimeRecord record in period)
            {
                Trace.Write("Record: ");
                Trace.Write(record.StartTime.Date);
                Trace.Write("\n");
                foreach (DateTime holiday in holidays)
                {
                    Trace.Write("\tHoliday: ");
                    Trace.Write(holiday.Date);
                    Trace.Write("\n");
                    if (record.StartTime.Date == holiday.Date || record.StartTime.DayOfWeek == DayOfWeek.Sunday || record.StartTime.DayOfWeek == DayOfWeek.Saturday)
                    {
                        record.IsHoliday = true;
                        break;
                    }
                }
                Trace.Write("Result: ");
                Trace.Write(record.IsHoliday);
                Trace.Write("\n");
            }

            // Assert
            // 14 Dec is NOT a holiday
            Assert.AreEqual(false, period[0].IsHoliday);

            // Saturday is a holiday
            Assert.AreEqual(true, period[3].IsHoliday);

            // Sunday is a holiday
            Assert.AreEqual(true, period[4].IsHoliday);
            
            // 25 Dec Chrismas Day is a holiday in NAT
            Assert.AreEqual(true, period[11].IsHoliday);

            // 27 Dec is a substitue Chrismas Day holiday in NSW
            Assert.AreEqual(true, period[13].IsHoliday);
        }
    }
}
