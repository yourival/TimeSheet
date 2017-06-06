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
    public class LeaveApplicationController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();

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
            //get manager droplist
            ViewBag.Manager = AdminController.GetManagerItems();
            ViewBag.LeaveType = LeaveApplication.GetLeaveTypeItems();
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
                    // Sum up total leave time
                    applicationVM.LeaveApplication.TotalLeaveTime += r.LeaveTime;

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
                        takenLeaves[index] -= timeRecord.LeaveTime;
                        timeRecord.LeaveTime = r.LeaveTime;
                        timeRecord.LeaveType = r.LeaveType;
                        contextDb.Entry(timeRecord).State = EntityState.Modified;
                    }
                    contextDb.SaveChanges();
                }

                // Update if exists, Add if not
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
                    application.TotalLeaveTime = applicationVM.LeaveApplication.TotalLeaveTime;
                    contextDb.Entry(application).State = EntityState.Modified;
                }
                contextDb.SaveChanges();
                
                // Update user leaves data in Db after submitting
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
            return RedirectToAction("Create");
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
                var newTimeRecord = (from r in contextDb.TimeRecords
                                            where DbFunctions.TruncateTime(r.RecordDate) == currentDate.Date &&
                                                  r.UserID == User.Identity.Name
                                            select r).FirstOrDefault();
                if (newTimeRecord == null)
                {
                    newTimeRecord = new TimeRecord(currentDate.Date);
                    newTimeRecord.UserID = User.Identity.Name;
                    newTimeRecord.LeaveType = leaveType;
                    PayPeriod.SetPublicHoliday(newTimeRecord);
                    newTimeRecord.LeaveTime = (newTimeRecord.IsHoliday ? 0 : 7.5);
                }
                if (!newTimeRecord.IsHoliday)
                newTimeRecords.Add(newTimeRecord);
            }
            applicationVM.TimeRecords = newTimeRecords;

            if (applicationVM.TimeRecords.Count == 0)
                return Content("No working days found.");

            return PartialView(@"~/Views/LeaveApplication/_Create.cshtml", applicationVM);
        }
    }
}
