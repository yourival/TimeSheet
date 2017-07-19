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
using System.Data.Entity.Infrastructure;

namespace TimeSheet.Controllers
{
    [Authorize]
    public class HRApplicationController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();
        public enum postRequestStatus { success, fail };

        // GET: LeaveApplication
        public ActionResult Index()
        {
            return View();
        }

        // GET: LeaveApplication/Leave
        public ActionResult Leave()
        {
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            List<LeaveBalance> LeaveBalances = new List<LeaveBalance>();
            //get manager droplist
            ViewBag.Manager = UserRoleSetting.GetManagerItems();
            for (int i = 0; i < 3; i++)
            {
                var availableLeave = contextDb.LeaveBalances.Find(User.Identity.Name, (_leaveType)i);
                LeaveBalances.Add(availableLeave == null ? new LeaveBalance() : availableLeave);
            }
            applicationVM.LeaveBalances = LeaveBalances;

            return PartialView("_Leave", applicationVM);
        }

        // GET: LeaveApplication/Casual
        public ActionResult Casual()
        {
            TimeSheetContainer model = new TimeSheetContainer();

            //get droplists of year and managers
            model.YearList = PayPeriod.GetYearItems();
            ViewBag.Manager = UserRoleSetting.GetManagerItems();

            return PartialView("_Casual", model);
        }

        // GET: LeaveApplication/ApplicationHistory
        public ActionResult ApplicationHistory()
        {
            List<LeaveApplication> applications = contextDb.LeaveApplications.Where(
                                                        a => a.UserID == User.Identity.Name).ToList();
            return View(applications);
        }

        public ActionResult ApplicationDetail(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            LeaveApplicationViewModel model = new LeaveApplicationViewModel();

            // Get the appliction from DB
            var application = contextDb.LeaveApplications
                                       .Include(a => a.Attachments)
                                       .SingleOrDefault(a => a.id == id);
            if (application == null)
            {
                return HttpNotFound("The application you request does not exist in our database. Please contact our IT support.");
            }
            else
            {
                model.LeaveApplication = application;
                model.TimeRecords = application.GetTimeRecords();


                model.LeaveApplication = application;

                return View(model);
            }
        }

        // GET: Year
        // POST: Year
        public ActionResult SelectYear(int? year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems((year == null) ? DateTime.Now.Year : year.Value);
            return PartialView("_SelectYear", model);
        }

        // POST: LeaveApplication/Leave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Leave(LeaveApplicationViewModel applicationVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Initialise variables
                    double[] appliedLeaveTimes = new double[3];
                    string originalBalances = String.Empty;

                    // Record Submitted Time
                    applicationVM.LeaveApplication.SubmittedTime = DateTime.Now;

                    // Calculate applied leave times and group by leavetype
                    foreach (var l in applicationVM.TimeRecords)
                    {
                        // Compassionate pay will take Sick leave balance
                        if (l.LeaveType == _leaveType.compassionatePay)
                        {
                            appliedLeaveTimes[(int)_leaveType.sick] += l.LeaveTime;
                        }
                        // Subtract balance for Sick Leave, Flexi Leave, and Annual Leave
                        else if ((int)l.LeaveType < 3)
                        {
                            appliedLeaveTimes[(int)l.LeaveType] += l.LeaveTime;
                        }
                        // Flexi hours will increase Flexi leave balance
                        else if (l.LeaveType == _leaveType.flexiHours)
                        {
                            appliedLeaveTimes[(int)_leaveType.flexi] -= l.LeaveTime;
                        }
                    }

                    // Update TimeRecords in Db
                    foreach (var r in applicationVM.TimeRecords)
                    {
                        // Configure Time Record if it's not a full day off
                        if (r.LeaveTime != 7.5)
                            r.SetAttendence(9, 17 - r.LeaveTime, 0.5);

                        // Sum up total leave time
                        applicationVM.LeaveApplication.TotalLeaveTime += r.LeaveTime;

                        // Try to fetch TimeRecord from Db if it exists
                        var timeRecord = (from a in contextDb.TimeRecords
                                          where DbFunctions.TruncateTime(a.RecordDate) == r.RecordDate.Date &&
                                                  a.UserID == r.UserID
                                          select a).FirstOrDefault();

                        // Update TimeRecord if exists, add if not
                        if (timeRecord == null)
                        {
                            contextDb.TimeRecords.Add(r);
                        }
                        else
                        {
                            // Record the difference of applied leave time and the balance in Db
                            // Compassionate pay will take Sick leaves balance
                            if (timeRecord.LeaveType == _leaveType.compassionatePay)
                            {
                                appliedLeaveTimes[(int)_leaveType.sick] -= timeRecord.LeaveTime;
                            }
                            // Subtract balance for Sick Leave, Flexi Leave, and Annual Leave
                            else if ((int)timeRecord.LeaveType < 3)
                            {
                                appliedLeaveTimes[(int)timeRecord.LeaveType] -= timeRecord.LeaveTime;
                            }

                            timeRecord.LeaveTime = r.LeaveTime;
                            timeRecord.LeaveType = r.LeaveType;
                            contextDb.Entry(timeRecord).State = EntityState.Modified;
                        }
                        //contextDb.SaveChanges();
                    }

                    // Transfer attachments to ViewModel
                    if (applicationVM.Attachments.Count != 0)
                    {
                        List<LeaveAttachment> files = new List<LeaveAttachment>();
                        foreach (var file in applicationVM.Attachments)
                        {
                            if (file != null && file.ContentLength > 0)
                            {
                                var attachment = new LeaveAttachment
                                {
                                    FileName = System.IO.Path.GetFileName(file.FileName),
                                    ContentType = file.ContentType
                                };
                                using (var reader = new System.IO.BinaryReader(file.InputStream))
                                {
                                    attachment.Content = reader.ReadBytes(file.ContentLength);
                                }
                                files.Add(attachment);
                            }
                        }
                        applicationVM.LeaveApplication.Attachments = files;
                    }

                    // Update user leaves balance in Db after submitting                    
                    for (int i = 0; i < 3; i++)
                    {
                        var LeaveBalance = contextDb.LeaveBalances.Find(User.Identity.Name, (_leaveType)i);
                        if (LeaveBalance == null)
                        {
                            originalBalances += "0.00";
                            LeaveBalance = new LeaveBalance()
                            {
                                LeaveType = (_leaveType)i,
                                UserID = User.Identity.Name,
                                AvailableLeaveHours = 0
                            };
                            contextDb.LeaveBalances.Add(LeaveBalance);
                        }
                        else
                        {
                            originalBalances += string.Format("{0:0.00}", LeaveBalance.AvailableLeaveHours);
                            LeaveBalance.AvailableLeaveHours -= appliedLeaveTimes[i];
                            contextDb.Entry(LeaveBalance).State = EntityState.Modified;
                        }
                        if (i != 2)
                            originalBalances += "/";
                        //contextDb.SaveChanges();
                    }

                    // Try to fetch Leaveapplication from DB if it exists
                    applicationVM.LeaveApplication.UserID = User.Identity.Name;
                    var application = (from a in contextDb.LeaveApplications
                                       where DbFunctions.TruncateTime(a.StartTime) == applicationVM.LeaveApplication.StartTime.Date &&
                                             DbFunctions.TruncateTime(a.EndTime) == applicationVM.LeaveApplication.EndTime.Date &&
                                             a.UserID == applicationVM.LeaveApplication.UserID
                                       select a).FirstOrDefault();

                    // Update LeaveApplication if exists, add if not
                    if (application == null)
                    {
                        applicationVM.LeaveApplication.status = _status.submited;
                        applicationVM.LeaveApplication.UserName = contextDb.ADUsers.Find(User.Identity.Name).UserName;
                        applicationVM.LeaveApplication.OriginalBalances = originalBalances;
                        contextDb.LeaveApplications.Add(applicationVM.LeaveApplication);
                    }
                    else
                    {
                        application.status = _status.modified;
                        application.leaveType = applicationVM.LeaveApplication.leaveType;
                        application.ManagerID = applicationVM.LeaveApplication.ManagerID;
                        application.Comment = applicationVM.LeaveApplication.Comment;
                        application.TotalLeaveTime = applicationVM.LeaveApplication.TotalLeaveTime;
                        contextDb.Entry(application).State = EntityState.Modified;
                    }
                    contextDb.SaveChanges();

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
                return RedirectToAction("PostRequest", new { status = postRequestStatus.success });
            }
            catch (RetryLimitExceededException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            return RedirectToAction("PostRequest", new { status = postRequestStatus.fail });
        }

        // POST: LeaveApplication/Casual
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Casual(TimeSheetContainer model)
        {
            try
            {
                TempData["model"] = model;
                if (ModelState.IsValid)
                {
                    double workingHours = 0;
                    string userId = User.Identity.Name;
                    for (int i = 0; i < model.TimeRecords.Count; i++)
                    {
                        // Add/Update each TimeRecords
                        DateTime recordDate = model.TimeRecords[i].RecordDate;
                        var record = contextDb.TimeRecords.Where(t => t.UserID == userId &&
                                                           t.RecordDate == recordDate).FirstOrDefault();
                        if (record == null)
                        {
                            contextDb.TimeRecords.Add(model.TimeRecords[i]);
                        }
                        else
                        {
                            record.StartTime = model.TimeRecords[i].StartTime;
                            record.EndTime = model.TimeRecords[i].EndTime;
                            record.LunchBreak = model.TimeRecords[i].LunchBreak;
                            contextDb.Entry(record).State = EntityState.Modified;
                        }
                        // Calculate total working hours
                        workingHours += model.TimeRecords[i].WorkHours;
                    }
                    model.TimeRecordForm.TotalWorkingHours = workingHours;
                    model.TimeRecordForm.SubmittedTime = DateTime.Now;
                    contextDb.SaveChanges();

                    // Add/Update TimeSheetForm data
                    var form = contextDb.TimeRecordForms.Where(t => t.UserID == userId &&
                                                           t.Year == model.TimeRecordForm.Year &&
                                                           t.Period == model.TimeRecordForm.Period).FirstOrDefault();
                    if (form == null)
                    {
                        model.TimeRecordForm.status = _status.submited;
                        model.TimeRecordForm.UserID = User.Identity.Name;
                        contextDb.TimeRecordForms.Add(model.TimeRecordForm);
                    }
                    else
                    {
                        form.status = _status.modified;
                        form.TotalWorkingHours = model.TimeRecordForm.TotalWorkingHours;
                        form.ManagerID = model.TimeRecordForm.ManagerID;
                        contextDb.Entry(form).State = EntityState.Modified;
                    }
                    contextDb.SaveChanges();

                    ////send email to manager
                    form = (from f in contextDb.TimeRecordForms
                            where f.Period == model.TimeRecordForm.Period
                            where f.Year == model.TimeRecordForm.Year
                            where f.UserID == model.TimeRecordForm.UserID
                            select f).FirstOrDefault();
                    if (form != null)
                    {
                        Task.Run(() => EmailSetting.SendEmail(form.ManagerID, string.Empty, "TimesheetApplication", form.TimeRecordFormId.ToString()));
                    }
                    return RedirectToAction("PostRequest", new { status = postRequestStatus.success });
                }
                //TempData["ErrorModel"] = ModelState.Values;
                return RedirectToAction("PostRequest", new { status = postRequestStatus.fail });
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        // GET: LeaveApplication/CreateLeaveList
        public ActionResult CreateLeaveList(DateTime start, DateTime end, _leaveType leaveType)
        {
            // Create new Leaveapplication
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();

            for (int i = 0; i <= (end - start).Days; i++)
            {
                // Create new timerecords
                DateTime currentDate = start.AddDays(i);
                var newTimeRecord = new TimeRecord(currentDate.Date);
                PayPeriod.SetPublicHoliday(newTimeRecord);
                if (!newTimeRecord.IsHoliday)
                {
                    newTimeRecord.SetAttendence(null, null, 0);
                    newTimeRecord.UserID = User.Identity.Name;
                    newTimeRecord.LeaveType = leaveType;
                    newTimeRecord.LeaveTime = 7.5;
                    newTimeRecords.Add(newTimeRecord);
                }
            }
            applicationVM.TimeRecords = newTimeRecords;

            if (applicationVM.TimeRecords.Count == 0)
                return Content("No working days were found.");

            return PartialView("_LeaveList", applicationVM);
        }

        // Get time records based on year period
        public ActionResult CreateCasualList(int year, int period)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.TimeRecordForm = new TimeRecordForm();
            model.TimeRecords = new List<TimeRecord>();

            DateTime firstPayDay = PayPeriod.GetStartDay(year, period);

            var form = (from f in contextDb.TimeRecordForms
                        where f.Year == year
                        where f.Period == period
                        where f.UserID == User.Identity.Name
                        select f).FirstOrDefault();
            if (form == null)
            {
                Startup.NoRecords = true;
                model.TimeRecordForm.Year = year;
                model.TimeRecordForm.Period = period;
                model.TimeRecordForm.UserID = User.Identity.Name;
            }
            else
            {
                Startup.NoRecords = false;
                model.TimeRecordForm = form;
            }

            for (int i = 0; i < 14; i++)
            {
                DateTime date = firstPayDay.AddDays(i);
                var record = (from r in contextDb.TimeRecords
                              where r.RecordDate == date
                              where r.UserID == User.Identity.Name
                              select r).FirstOrDefault();
                if (record == null)
                {
                    TimeRecord r = new TimeRecord(date);
                    r.UserID = User.Identity.Name;
                    PayPeriod.SetPublicHoliday(r);
                    if (r.IsHoliday)
                        r.SetAttendence(null, null, 0);
                    model.TimeRecords.Add(r);
                }
                else
                {
                    PayPeriod.SetPublicHoliday(record);
                    model.TimeRecords.Add(record);
                }
            }

            return PartialView("_CasualList", model);
        }

        public ActionResult PostRequest(postRequestStatus status)
        {
            switch (status)
            {
                case postRequestStatus.fail:
                    ViewBag.Title = "INVAILD APPLICATION";
                    ViewBag.Body = "Some data in the application are not valid.<br />" +
                                   "Please try again and check them before submitting.<br />" +
                                   "If the problem keeps occurring, please contact our IT support.";
                    break;
                case postRequestStatus.success:
                    ViewBag.Title = "DONE!";
                    ViewBag.Body = "Your application was sent successfully.<br />" +
                                   "You will recieve a confirmation email within a couple of minutes. <br />" +
                                   "Please wait for the manager to process your application.";
                    break;
                default:
                    break;
            }
            return View();
        }
    }
}
