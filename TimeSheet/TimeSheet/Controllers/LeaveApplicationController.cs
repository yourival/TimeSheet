using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Diagnostics;

namespace TimeSheet.Controllers
{
    public class LeaveApplicationController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();

        // GET: LeaveApplication
        public ActionResult Index()
        {
            return View();
        }

        // GET: LeaveApplication/Details/5
        public ActionResult Details(int id)
        {
            return View(contextDb.LeaveApplications);
        }

        // GET: LeaveApplication/Create
        [Authorize]
        public ActionResult Create()
        {
            // Fetch Available Leaves in DB
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            List<LeaveRecord> leaveRecords = new List<LeaveRecord>();
            for (int i = 1; i < 4; i++)
            {
                var availableLeave = contextDb.LeaveRecords.Find(User.Identity.Name, (_leaveType)i);
                leaveRecords.Add(availableLeave == null ? new LeaveRecord() : availableLeave);
            }
            applicationVM.LeaveRecords = leaveRecords;

            return View(applicationVM);
        }

        // POST: LeaveApplication/Create
        [HttpPost]
        public ActionResult Create(LeaveApplicationViewModel applicationVM)
        {
            try
            {
                TimeSpan[] takenLeaves = new TimeSpan[3];
                foreach (var l in applicationVM.TimeRecords)
                {
                    int index = (int)l.LeaveType-1;
                    takenLeaves[index] = takenLeaves[index].Add(l.LeaveTime);
                }

                // Try to fetch Leaveapplication from DB if it exists
                applicationVM.LeaveApplication.status = _status.submited;
                applicationVM.LeaveApplication.UserID = User.Identity.Name;
                var application = (from a in contextDb.LeaveApplications
                                                 where DbFunctions.TruncateTime(a.StartTime) == applicationVM.LeaveApplication.StartTime.Date &&
                                                       DbFunctions.TruncateTime(a.EndTime) == applicationVM.LeaveApplication.EndTime.Date &&
                                                       a.UserID == applicationVM.LeaveApplication.UserID
                                                 select a).FirstOrDefault();

                // Update if exists, Add if not
                if (application == null)
                {
                    contextDb.LeaveApplications.Add(applicationVM.LeaveApplication);
                }
                else
                {
                    application.leaveType = applicationVM.LeaveApplication.leaveType;
                    application.ManagerID = applicationVM.LeaveApplication.ManagerID;
                    contextDb.Entry(application).State = EntityState.Modified;
                }
                contextDb.SaveChanges();

                foreach(var r in applicationVM.TimeRecords)
                {
                    // Try to fetch TimeRecord from DB if it exists
                    var timeRecord = (from a in contextDb.TimeRecords
                                         where DbFunctions.TruncateTime(a.RecordDate) == r.RecordDate.Date &&
                                                 a.UserID == r.UserID
                                         select a).FirstOrDefault();

                    // Update if exists, Add if not
                    if (timeRecord == null)
                    {
                        contextDb.TimeRecords.Add(r);
                    }
                    else
                    {
                        int index = (int)timeRecord.LeaveType - 1;
                        takenLeaves[index] = takenLeaves[index].Subtract(timeRecord.LeaveTime);
                        timeRecord.LeaveTime = r.LeaveTime;
                        timeRecord.LeaveType = r.LeaveType;
                        contextDb.Entry(timeRecord).State = EntityState.Modified;
                    }
                    contextDb.SaveChanges();
                }
                
                // Update user leaves data in Db after submitting
                for (int i = 1; i < 4; i++)
                {
                    var leaveRecord = contextDb.LeaveRecords.Find(User.Identity.Name, (_leaveType)i);
                    if (leaveRecord == null)
                    {
                        leaveRecord = new LeaveRecord();
                        leaveRecord.LeaveType = (_leaveType)i;
                        leaveRecord.UserID = User.Identity.Name;
                        leaveRecord.AvailableLeaveTime = leaveRecord.AvailableLeaveTime.Subtract(takenLeaves[i-1]);
                        contextDb.LeaveRecords.Add(leaveRecord);
                    }
                    else
                    {
                        leaveRecord.AvailableLeaveTime = leaveRecord.AvailableLeaveTime.Subtract(takenLeaves[i-1]);
                        contextDb.Entry(leaveRecord).State = EntityState.Modified;
                    }
                    contextDb.SaveChanges();
                }
                return View();
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                return View();
            }
        }

        // GET: LeaveApplication/CreateLeaveForm
        public ActionResult CreateLeaveForm(DateTime start, DateTime end, _leaveType leaveType)
        {
            // Try to fetch Leaveapplication from DB if it exists
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();

            for (int i = 0; i <= (end - start).Days; i++)
            {
                // Fetch each timerecord in DB if it exists
                DateTime currentDate = start.AddDays(i);
                var newTimeRecord = (from r in contextDb.TimeRecords
                                            where DbFunctions.TruncateTime(r.RecordDate) == currentDate.Date &&
                                                  r.UserID == User.Identity.Name
                                            select r).FirstOrDefault();
                if (newTimeRecord == null)
                {
                    newTimeRecord = new TimeRecord(currentDate.Date);
                    newTimeRecord.UserID = User.Identity.Name;
                    newTimeRecord.LeaveType = leaveType;
                    newTimeRecord.LeaveTime = new TimeSpan(7, 30, 0);
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                }
                newTimeRecords.Add(newTimeRecord);
            }
            applicationVM.TimeRecords = newTimeRecords;
            
            return PartialView(@"~/Views/LeaveApplication/_Create.cshtml", applicationVM);
        } 
    }
}
