using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;
using System.Data.Entity;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Threading.Tasks;

namespace TimeSheet.Controllers
{
    [Authorize]
    public class LeaveApplicationController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();

        // GET: LeaveApplication
        public ActionResult Index()
        {
            return View();
        }

        // GET: LeaveApplication/_Leave
        public ActionResult Leave()
        {
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            List<LeaveRecord> leaveRecords = new List<LeaveRecord>();
            //get manager droplist
            ViewBag.Manager = Manager.GetManagerItems();
            ViewBag.LeaveType = LeaveApplication.GetLeaveTypeItems();
            for (int i = 1; i < 4; i++)
            {
                var availableLeave = contextDb.LeaveRecords.Find(User.Identity.Name, (_leaveType)i);
                leaveRecords.Add(availableLeave == null ? new LeaveRecord() : availableLeave);
            }
            applicationVM.LeaveRecords = leaveRecords;

            return PartialView(@"~/Views/LeaveApplication/_Leave.cshtml", applicationVM);
        }

        // GET: LeaveApplication/_Casual
        public ActionResult Casual()
        {
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            List<LeaveRecord> leaveRecords = new List<LeaveRecord>();
            //get manager droplist
            ViewBag.Manager = Manager.GetManagerItems();
            applicationVM.LeaveRecords = leaveRecords;

            return PartialView(@"~/Views/LeaveApplication/_Casual.cshtml", applicationVM);
        }

        // POST: LeaveApplication/_Leave
        [HttpPost]
        public async Task<ActionResult> Index(LeaveApplicationViewModel applicationVM)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Create");
            }

            try
            {
                double[] takenLeaves = new double[3];
                foreach (var l in applicationVM.TimeRecords)
                {
                    int index = (int)l.LeaveType-1;
                    takenLeaves[index] += l.LeaveTime;
                }

                // Try to fetch Leaveapplication from DB if it exists
                applicationVM.LeaveApplication.UserID = User.Identity.Name;
                var application = (from a in contextDb.LeaveApplications
                                                 where DbFunctions.TruncateTime(a.StartTime) == applicationVM.LeaveApplication.StartTime.Date &&
                                                       DbFunctions.TruncateTime(a.EndTime) == applicationVM.LeaveApplication.EndTime.Date &&
                                                       a.UserID == applicationVM.LeaveApplication.UserID
                                                 select a).FirstOrDefault();

                foreach(var r in applicationVM.TimeRecords)
                {
                    // Configure time record if it's not a full day off
                    if(r.LeaveTime != 7.5)
                        r.SetAttendence(9, 17 - r.LeaveTime, 0.5);

                    // Sum up total leave time
                    applicationVM.LeaveApplication.TotalTime += r.LeaveTime;

                    // Try to fetch TimeRecord from DB if it exists
                    var timeRecord = (from a in contextDb.TimeRecords
                                         where DbFunctions.TruncateTime(a.RecordDate) == r.RecordDate.Date &&
                                                 a.UserID == r.UserID
                                         select a).FirstOrDefault();

                    // Update TimeRecord if exists, Add if not
                    if (timeRecord == null)
                    {
                        contextDb.TimeRecords.Add(r);
                    }
                    else
                    {
                        int index = (int)timeRecord.LeaveType - 1;
                        takenLeaves[index] -= timeRecord.LeaveTime;
                        timeRecord.LeaveTime = r.LeaveTime;
                        timeRecord.LeaveType = r.LeaveType;
                        contextDb.Entry(timeRecord).State = EntityState.Modified;
                    }
                    contextDb.SaveChanges();
                }

                // Update LeaveApplication if exists, add if not
                if (application == null)
                {
                    applicationVM.LeaveApplication.status = _status.submited;
                    contextDb.LeaveApplications.Add(applicationVM.LeaveApplication);
                }
                else
                {
                    application.status = _status.modified;
                    application.leaveType = applicationVM.LeaveApplication.leaveType;
                    application.ManagerID = applicationVM.LeaveApplication.ManagerID;
                    application.Comment = applicationVM.LeaveApplication.Comment;
                    application.TotalTime = applicationVM.LeaveApplication.TotalTime;
                    contextDb.Entry(application).State = EntityState.Modified;
                }
                contextDb.SaveChanges();
                
                // Update user leaves data in Db after submitting if it's leave application
                if(applicationVM.LeaveApplication.leaveType != _leaveType.none)
                {
                    for (int i = 1; i < 4; i++)
                    {
                        var leaveRecord = contextDb.LeaveRecords.Find(User.Identity.Name, (_leaveType)i);
                        if (leaveRecord == null)
                        {
                            leaveRecord = new LeaveRecord();
                            leaveRecord.LeaveType = (_leaveType)i;
                            leaveRecord.UserID = User.Identity.Name;
                            leaveRecord.AvailableLeaveHours -= takenLeaves[i-1];
                            contextDb.LeaveRecords.Add(leaveRecord);
                        }
                        else
                        {
                            leaveRecord.AvailableLeaveHours -= takenLeaves[i-1];
                            contextDb.Entry(leaveRecord).State = EntityState.Modified;
                        }
                        contextDb.SaveChanges();
                    }
                }

                // Send an email to manager
                var applicationModel = (from a in contextDb.LeaveApplications
                                        where DbFunctions.TruncateTime(a.StartTime) == applicationVM.LeaveApplication.StartTime.Date &&
                                              DbFunctions.TruncateTime(a.EndTime) == applicationVM.LeaveApplication.EndTime.Date &&
                                              a.UserID == applicationVM.LeaveApplication.UserID
                                        select a).FirstOrDefault();

                if (applicationModel != null)
                {
                    Task.Run(() => EmailSetting.SendEmail(applicationModel.ManagerID, string.Empty, "LeaveApplication", applicationModel.id.ToString()));
                }    
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
            return RedirectToAction("Index");
        }

        // GET: LeaveApplication/CreateLeaveForm
        public ActionResult CreateLeaveForm(DateTime start, DateTime end, _leaveType leaveType)
        {
            // Try to fetch Leaveapplication from DB if it exists
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();
            ViewBag.LeaveType = LeaveApplication.GetLeaveTypeItems();

            for (int i = 0; i <= (end - start).Days; i++)
            {
                // Fetch each timerecord in DB if it exists
                DateTime currentDate = start.AddDays(i);
                var newTimeRecord = new TimeRecord(currentDate.Date);
                newTimeRecord.SetAttendence(null, null, 0);
                newTimeRecord.UserID = User.Identity.Name;
                newTimeRecord.LeaveType = leaveType;
                newTimeRecord.LeaveTime = (leaveType == 0) ? 0 : 7.5;
                newTimeRecord.WorkHours = 0;
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                if (!newTimeRecord.IsHoliday)
                    newTimeRecords.Add(newTimeRecord);
            }
            applicationVM.TimeRecords = newTimeRecords;

            if (applicationVM.TimeRecords.Count == 0)
                return Content("No working days were found.");

            if(leaveType == 0)
                return PartialView(@"~/Views/LeaveApplication/_CasualList.cshtml", applicationVM);
            else
                return PartialView(@"~/Views/LeaveApplication/_LeaveList.cshtml", applicationVM);
        }
    }
}
