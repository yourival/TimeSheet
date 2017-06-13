using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.Threading.Tasks;

namespace TimeSheet.Models
{
    public class TimeSheetInitializer : CreateDatabaseIfNotExists<TimeSheetDb>
    {
        // Puts sample data into the database
        protected override void Seed(TimeSheetDb context)
        {
            // LeaveApplications
            List<LeaveApplication> applications = new List<LeaveApplication>();
            List<TimeRecord> timeRecords = new List<TimeRecord>();
            DateTime startDate = new DateTime(2017, 1, 1);
            for(int i = 0; i < 10; i++)
            {
                LeaveApplication newApplication = new LeaveApplication();
                newApplication.UserID = "r.lin@m.nantien.edu.au";
                newApplication.UserName = "Robin Lin";
                newApplication.StartTime = startDate.AddDays(i * 4);
                newApplication.EndTime = startDate.AddDays(i * 4 + 3);
                newApplication.leaveType = (_leaveType)(i % 4);
                newApplication.status = (_status)(i % 4);
                newApplication.TotalLeaveTime = 30;
                applications.Add(newApplication);

                // TimeRecords
                for (int j = 0; j < 4; j++)
                {
                    TimeRecord newTimeRecord = new TimeRecord(startDate.AddDays(i * 4 + j));
                    newTimeRecord.UserID = "r.lin@m.nantien.edu.au";
                    newTimeRecord.LeaveType = (_leaveType)(i % 4);
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                    newTimeRecord.LeaveTime = (newTimeRecord.IsHoliday ? 0 : 7.5);
                    timeRecords.Add(newTimeRecord);
                }
            }
            for (int i = 0; i < 10; i++)
            {
                LeaveApplication newApplication = new LeaveApplication();
                newApplication.UserID = "y.ben@m.nantien.edu.au";
                newApplication.UserName = "Rita Ben";
                newApplication.StartTime = startDate.AddDays(i * 3);
                newApplication.EndTime = startDate.AddDays(i * 3 + 2);
                newApplication.leaveType = (_leaveType)((i + 1) % 4);
                newApplication.status = (_status)(i % 4 + 1);
                newApplication.TotalLeaveTime = 22.5;
                applications.Add(newApplication);

                // TimeRecords
                for (int j = 0; j < 3; j++)
                {
                    TimeRecord newTimeRecord = new TimeRecord(startDate.AddDays(i * 3 + j));
                    newTimeRecord.UserID = "y.ben@m.nantien.edu.au";
                    newTimeRecord.LeaveType = (_leaveType)((i + 1) % 4);
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                    newTimeRecord.LeaveTime = (newTimeRecord.IsHoliday ? 0 : 7.5);
                    timeRecords.Add(newTimeRecord);
                }
            }

            for (int i = 0; i < 10; i++)
            {
                LeaveApplication newApplication = new LeaveApplication();
                newApplication.UserID = "d.yang@m.nantien.edu.au";
                newApplication.UserName = "Dawen Yang";
                newApplication.StartTime = startDate.AddDays(i * 3);
                newApplication.EndTime = startDate.AddDays(i * 3 + 2);
                newApplication.leaveType = (_leaveType)((i +1) % 4);
                newApplication.status = (_status)(i % 4 + 1);
                newApplication.TotalLeaveTime = 22.5;
                applications.Add(newApplication);

                // TimeRecords
                for (int j = 0; j < 3; j++)
                {
                    TimeRecord newTimeRecord = new TimeRecord(startDate.AddDays(i * 3 + j));
                    newTimeRecord.UserID = "d.yang@m.nantien.edu.au";
                    newTimeRecord.LeaveType = (_leaveType)((i + 1) % 4);
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                    newTimeRecord.LeaveTime = (newTimeRecord.IsHoliday ? 0 : 7.5);
                    timeRecords.Add(newTimeRecord);
                }
            }
            applications.ForEach(a => context.LeaveApplications.Add(a));
            timeRecords.ForEach(t => context.TimeRecords.Add(t));

            // Initialise Leaves
            context.LeaveRecords.Add(new LeaveRecord()
            {
                UserID = "r.lin@m.nantien.edu.au",
                LeaveType = _leaveType.annual,
                AvailableLeaveHours = 100
            });
            context.LeaveRecords.Add(new LeaveRecord()
            {
                UserID = "r.lin@m.nantien.edu.au",
                LeaveType = _leaveType.flexi,
                AvailableLeaveHours = 7.5
            });
            context.LeaveRecords.Add(new LeaveRecord()
            {
                UserID = "r.lin@m.nantien.edu.au",
                LeaveType = _leaveType.sick,
                AvailableLeaveHours = 20
            });
            context.SaveChanges();

            Task.Run(() => ADUser.GetADUser());

        }
    }
}