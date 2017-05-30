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
        public async Task<ActionResult> Create(LeaveApplicationViewModel applicationVM, FormCollection form)
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
                    applicationVM.LeaveApplication.ManagerID = form["manager"].ToString();
                    applicationVM.LeaveApplication.status = _status.submited;
                    contextDb.LeaveApplications.Add(applicationVM.LeaveApplication);
                }
                else
                {
                    application.status = _status.modified;
                    application.leaveType = applicationVM.LeaveApplication.leaveType;
                    application.ManagerID = form["manager"].ToString();
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
                    string link = "http://localhost/Admin/Approval/ApplicationDetails/" + applicationModel.id;
                    EmailSetting model = adminDb.EmailSetting.FirstOrDefault();
                    string body = "<p>Message: </p><p>{0}</p><p>Link: </p><a href='{1}'>{1}</a>";
                    var message = new MailMessage();
                    message.To.Add(new MailAddress(applicationModel.ManagerID));
                    message.From = new MailAddress(model.FromEmail);
                    message.Subject = model.Subject;
                    message.Body = string.Format(body, model.Message, link);
                    message.IsBodyHtml = true;

                    using (var smtp = new SmtpClient())
                    {
                        var credential = new NetworkCredential
                        {
                            UserName = model.FromEmail,
                            Password = model.Password
                        };
                        smtp.Credentials = credential;
                        smtp.Host = model.SMTPHost;
                        smtp.Port = model.SMTPPort;
                        smtp.EnableSsl = model.EnableSsl;
                        await smtp.SendMailAsync(message);
                    }
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
            
            return PartialView(@"~/Views/LeaveApplication/_Create.cshtml", applicationVM);
        } 
    }
}
