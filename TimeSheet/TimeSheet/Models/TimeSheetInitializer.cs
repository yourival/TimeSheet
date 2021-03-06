﻿using System;
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
            Task.Run(() => ADUser.GetADUser());
            for (int i = 0; i < 10; i++)
            {
                LeaveApplication newApplication = new LeaveApplication();
                newApplication.UserID = "r.lin@m.nantien.edu.au";
                newApplication.UserName = "Robin Lin";
                newApplication.ManagerIDs = "r.lin@m.nantien.edu.au";
                newApplication.StartTime = startDate.AddDays(i * 4);
                newApplication.EndTime = startDate.AddDays(i * 4 + 3);
                newApplication.leaveType = (_leaveType)(i % 3);
                newApplication.status = (_status)(i % 4);
                newApplication.SubmittedTime = new DateTime(2017, 2, 1);
                if (newApplication.status == _status.approved || newApplication.status == _status.rejected)
                {
                    newApplication.ApprovedTime = new DateTime(2017, 1, 10);
                    newApplication.ApprovedBy = newApplication.ManagerIDs;
                }
                newApplication.TotalLeaveTime = 30;
                applications.Add(newApplication);

                // TimeRecords
                for (int j = 0; j < 4; j++)
                {
                    TimeRecord newTimeRecord = new TimeRecord(startDate.AddDays(i * 4 + j));
                    newTimeRecord.UserID = "r.lin@m.nantien.edu.au";
                    newTimeRecord.LeaveType = (_leaveType)(i % 3);
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                    if (!newTimeRecord.IsHoliday)
                    {
                        newTimeRecord.LeaveTime = 7.5;
                        newTimeRecord.SetAttendence(null, null, 0);
                        timeRecords.Add(newTimeRecord);
                    }

                }
            }

            for (int i = 0; i < 10; i++)
            {
                LeaveApplication newApplication = new LeaveApplication();
                newApplication.UserID = "y.ben@m.nantien.edu.au";
                newApplication.UserName = "Rita Ben";
                newApplication.ManagerIDs = "r.lin@m.nantien.edu.au";
                newApplication.StartTime = startDate.AddDays(i * 3);
                newApplication.EndTime = startDate.AddDays(i * 3 + 2);
                newApplication.leaveType = (_leaveType)((i + 1) % 3);
                newApplication.status = (_status)(i % 4 + 1);
                newApplication.SubmittedTime = new DateTime(2017, 1, 10);
                if (newApplication.status == _status.approved || newApplication.status == _status.rejected)
                {
                    newApplication.ApprovedTime = new DateTime(2017, 1, 10);
                    newApplication.ApprovedBy = newApplication.ManagerIDs;
                }
                newApplication.TotalLeaveTime = 22.5;
                applications.Add(newApplication);

                // TimeRecords
                for (int j = 0; j < 3; j++)
                {
                    TimeRecord newTimeRecord = new TimeRecord(startDate.AddDays(i * 3 + j));
                    newTimeRecord.UserID = "y.ben@m.nantien.edu.au";
                    newTimeRecord.LeaveType = (_leaveType)((i + 1) % 3);
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                    if (!newTimeRecord.IsHoliday)
                    {
                        newTimeRecord.LeaveTime = 7.5;
                        newTimeRecord.SetAttendence(null, null, 0);
                        timeRecords.Add(newTimeRecord);
                    }
                }
            }

            for (int i = 0; i < 10; i++)
            {
                LeaveApplication newApplication = new LeaveApplication();
                newApplication.UserID = "d.yang@m.nantien.edu.au";
                newApplication.UserName = "Dawen Yang";
                newApplication.ManagerIDs = "r.lin@m.nantien.edu.au";
                newApplication.StartTime = startDate.AddDays(i * 3);
                newApplication.EndTime = startDate.AddDays(i * 3 + 2);
                newApplication.leaveType = (_leaveType)((i +1) % 3);
                newApplication.status = (_status)(i % 4 + 1);
                newApplication.SubmittedTime = new DateTime(2017, 1, 1);
                if (newApplication.status == _status.approved || newApplication.status == _status.rejected)
                {
                    newApplication.ApprovedTime = new DateTime(2017, 1, 10);
                    newApplication.ApprovedBy = newApplication.ManagerIDs;
                }
                newApplication.TotalLeaveTime = 22.5;
                applications.Add(newApplication);

                // TimeRecords
                for (int j = 0; j < 3; j++)
                {
                    TimeRecord newTimeRecord = new TimeRecord(startDate.AddDays(i * 3 + j));
                    newTimeRecord.UserID = "d.yang@m.nantien.edu.au";
                    newTimeRecord.LeaveType = (_leaveType)((i + 1) % 3);
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                    if (!newTimeRecord.IsHoliday)
                    {
                        newTimeRecord.LeaveTime = 7.5;
                        newTimeRecord.SetAttendence(null, null, 0);
                        timeRecords.Add(newTimeRecord);
                    }
                }
            }
            applications.ForEach(a => context.LeaveApplications.Add(a));
            timeRecords.ForEach(t => context.TimeRecords.Add(t));
            context.SaveChanges();

            // Initialise Leaves
            context.LeaveBalances.Add(new LeaveBalance
            {
                UserID = "r.lin@m.nantien.edu.au",
                LeaveType = _leaveType.annual,
                AvailableLeaveHours = 100.39
            });
            context.LeaveBalances.Add(new LeaveBalance
            {
                UserID = "r.lin@m.nantien.edu.au",
                LeaveType = _leaveType.flexi,
                AvailableLeaveHours = 22.5
            });
            context.LeaveBalances.Add(new LeaveBalance
            {
                UserID = "r.lin@m.nantien.edu.au",
                LeaveType = _leaveType.sick,
                AvailableLeaveHours = 20
            });
            context.LeaveBalances.Add(new LeaveBalance
            {
                UserID = "d.yang@m.nantien.edu.au",
                LeaveType = _leaveType.annual,
                AvailableLeaveHours = 123
            });
            context.LeaveBalances.Add(new LeaveBalance
            {
                UserID = "d.yang@m.nantien.edu.au",
                LeaveType = _leaveType.flexi,
                AvailableLeaveHours = 34
            });
            context.LeaveBalances.Add(new LeaveBalance
            {
                UserID = "d.yang@m.nantien.edu.au",
                LeaveType = _leaveType.sick,
                AvailableLeaveHours = 1.53
            });
            context.ADUsers.Add(new ADUser
            {
                UserName = "Robin Lin",
                Email = "r.lin@m.nantien.edu.au",
                JobCode = "Lecturer",
                Department = "IT"
            });
            context.ADUsers.Add(new ADUser
            {
                UserName = "Dawen Yang",
                Email = "d.yang@m.nantien.edu.au",
                JobCode = "Lecturer",
                Department = "IT"
            });
            context.SaveChanges();


        }
    }
}