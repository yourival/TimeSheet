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
            List<LeaveBalance> LeaveBalances = new List<LeaveBalance>();
            //get manager droplist
            ViewBag.Manager = Manager.GetManagerItems();
            ViewBag.LeaveType = LeaveApplication.GetLeaveTypeItems();
            for (int i = 1; i < 4; i++)
            {
                var availableLeave = contextDb.LeaveBalances.Find(User.Identity.Name, (_leaveType)i);
                LeaveBalances.Add(availableLeave == null ? new LeaveBalance() : availableLeave);
            }
            applicationVM.LeaveBalances = LeaveBalances;

            return PartialView(@"~/Views/LeaveApplication/_Leave.cshtml", applicationVM);
        }

        // GET: LeaveApplication/_Casual
        public ActionResult Casual()
        {
            int year = DateTime.Now.Year;
            int period = (int)(DateTime.Now - PayPeriod.FirstPayDayOfYear(year)).Days / 14 + 2;
            TimeSheetContainer model = CreateCasualList(year, period);
            model.YearList = PayPeriod.GetYearItems();
            //get manager droplist
            ViewBag.Manager = Manager.GetManagerItems();

            return PartialView(@"~/Views/LeaveApplication/_Casual.cshtml", model);
        }

        // GET: Year
        // POST: Year
        public ActionResult SelectYear(int? year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems((year == null) ? DateTime.Now.Year : year.Value);
            return PartialView("_SelectYear", model);
        }

        //Get year period user selected
        public ActionResult GetTimeRecords(string text)
        {
            string[] words = text.Split('.');
            var y = int.Parse(words[0]);
            var p = int.Parse(words[1]);
            var model = CreateCasualList(y, p);
            return PartialView("_CasualList", model);
        }

        // POST: LeaveApplication/_Leave
        [HttpPost]
        public async Task<ActionResult> Leave(LeaveApplicationViewModel applicationVM)
        {           

            try
            {
                if (ModelState.IsValid)
                {
                    double[] takenLeaves = new double[3];
                    foreach (var l in applicationVM.TimeRecords)
                    {
                        int index = (int)l.LeaveType-1;
                        takenLeaves[index] += l.LeaveTime;
                    }

                    // Transfer attachments to ViewModel
                    if (applicationVM.Attachments.Count > 0)
                    {
                        List<LeaveAttachment> files = new List<LeaveAttachment>();
                        foreach (var file in applicationVM.Attachments)
                        {
                            if (file.ContentLength > 0)
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
                            var LeaveBalance = contextDb.LeaveBalances.Find(User.Identity.Name, (_leaveType)i);
                            if (LeaveBalance == null)
                            {
                                LeaveBalance = new LeaveBalance();
                                LeaveBalance.LeaveType = (_leaveType)i;
                                LeaveBalance.UserID = User.Identity.Name;
                                LeaveBalance.AvailableLeaveHours -= takenLeaves[i-1];
                                contextDb.LeaveBalances.Add(LeaveBalance);
                            }
                            else
                            {
                                LeaveBalance.AvailableLeaveHours -= takenLeaves[i-1];
                                contextDb.Entry(LeaveBalance).State = EntityState.Modified;
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
            }
            catch (RetryLimitExceededException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
            }
            return RedirectToAction("Index");
        }
        
        // POST: LeaveApplication/_Casual
        [HttpPost]
        public ActionResult Casual(TimeSheetContainer model)
        {
            if (Startup.NoRecords == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        for (int i = 0; i < model.TimeRecords.Count; i++)
                        {
                            contextDb.TimeRecords.Add(model.TimeRecords[i]);
                        }
                        model.TimeRecordForm.FormStatus = TimeRecordForm._formstatus.modified;
                        model.TimeRecordForm.SumbitStatus = TimeRecordForm._sumbitstatus.saved;
                        model.TimeRecordForm.SubmitTime = DateTime.Now;

                        //Calculate the total working hours to current date
                        double workingHours = 0;
                        DateTime current = DateTime.Now.Date;
                        for (int i = 0; i <= Convert.ToInt32((current - model.TimeRecords[0].RecordDate).TotalDays); i++)
                        {
                            workingHours += model.TimeRecords[i].WorkHours;
                        }
                        model.TimeRecordForm.TotalWorkingHours = workingHours;

                        model.TimeRecordForm.TotalLeaveHours = 0;
                        contextDb.TimeRecordForms.Add(model.TimeRecordForm);
                        contextDb.SaveChanges();
                    }
                    return RedirectToAction("Index", new { message = 3 });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        for (int i = 0; i < model.TimeRecords.Count; i++)
                        {
                            contextDb.TimeRecords.Attach(model.TimeRecords[i]);
                            var entry = contextDb.Entry(model.TimeRecords[i]);
                            entry.Property(e => e.StartTime).IsModified = true;
                            entry.Property(e => e.LunchBreak).IsModified = true;
                            entry.Property(e => e.EndTime).IsModified = true;
                            entry.Property(e => e.Flexi).IsModified = true;
                            contextDb.SaveChanges();
                        }
                        model.TimeRecordForm.FormStatus = TimeRecordForm._formstatus.modified;
                        model.TimeRecordForm.SumbitStatus = TimeRecordForm._sumbitstatus.saved;

                        //Calculate the total working hours to current date
                        double workingHours = 0;
                        DateTime current = DateTime.Now.Date;
                        for (int i = 0; i <= Convert.ToInt32((current - model.TimeRecords[0].RecordDate).TotalDays); i++)
                        {
                            workingHours += model.TimeRecords[i].WorkHours;
                        }
                        model.TimeRecordForm.TotalWorkingHours = workingHours;

                        model.TimeRecordForm.TotalLeaveHours = 0;
                        model.TimeRecordForm.SubmitTime = DateTime.Now;
                        contextDb.TimeRecordForms.Attach(model.TimeRecordForm);
                        contextDb.Entry(model.TimeRecordForm).State = EntityState.Modified;
                        contextDb.SaveChanges();
                    }
                    return RedirectToAction("Index", new { message = 3 });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        // GET: LeaveApplication/CreateLeaveForm
        public ActionResult CreateLeaveList(DateTime start, DateTime end, _leaveType leaveType)
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
                PayPeriod.SetPublicHoliday(newTimeRecord);
                if (!newTimeRecord.IsHoliday)
                    newTimeRecords.Add(newTimeRecord);
            }
            applicationVM.TimeRecords = newTimeRecords;

            if (applicationVM.TimeRecords.Count == 0)
                return Content("No working days were found.");
            
            return PartialView(@"~/Views/LeaveApplication/_LeaveList.cshtml", applicationVM);
        }

        //get time records based on year period 
        private TimeSheetContainer CreateCasualList(int year, int period)
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
            return model;
        }
    }
}
