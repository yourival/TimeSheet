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
            for (int i = 0; i < 3; i++)
            {
                var availableLeave = contextDb.LeaveBalances.Find(User.Identity.Name, (_leaveType)i);
                LeaveBalances.Add(availableLeave == null ? new LeaveBalance() : availableLeave);
            }
            applicationVM.LeaveBalances = LeaveBalances;

            return PartialView("_Leave", applicationVM);
        }

        // GET: LeaveApplication/_Casual
        public ActionResult Casual()
        {
            //int year = DateTime.Now.Year;
            //int period = (int)(DateTime.Now - PayPeriod.FirstPayDayOfYear(year)).Days / 14 + 2;
            TimeSheetContainer model = new TimeSheetContainer();
            model.YearList = PayPeriod.GetYearItems();
            //get manager droplist
            ViewBag.Manager = Manager.GetManagerItems();

            return PartialView("_Casual", model);
        }

        // GET: Year
        // POST: Year
        public ActionResult SelectYear(int? year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems((year == null) ? DateTime.Now.Year : year.Value);
            return PartialView("_SelectYear", model);
        }

        // POST: LeaveApplication/_Leave
        [HttpPost]
        public async Task<ActionResult> Leave(LeaveApplicationViewModel applicationVM)
        {           
            try
            {
                if (ModelState.IsValid)
                {
                    double[] appliedLeaveTimes = new double[3];
                    foreach (var l in applicationVM.TimeRecords)
                    {
                        // Compassionate pay will take Sick leaves balance
                        if(l.LeaveType == _leaveType.compassionatePay)
                        {
                            appliedLeaveTimes[(int)_leaveType.sick] += l.LeaveTime;
                        }
                        // Subtract balance for Sick Leave, Flexi Leave, and Annual Leave
                        else if((int)l.LeaveType < 3)
                        {
                            appliedLeaveTimes[(int)l.LeaveType] += l.LeaveTime;
                        }
                    }

                    // Update Time Records in Db
                    foreach (var r in applicationVM.TimeRecords)
                    {
                        // Configure Time Record if it's not a full day off
                        if(r.LeaveTime != 7.5)
                            r.SetAttendence(9, 17 - r.LeaveTime, 0.5);

                        // Sum up total leave time
                        applicationVM.LeaveApplication.TotalTime += r.LeaveTime;

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
                        contextDb.SaveChanges();
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

                    // Update user leaves balance in Db after submitting                    
                    for (int i = 0; i < 3; i++)
                    {
                        var LeaveBalance = contextDb.LeaveBalances.Find(User.Identity.Name, (_leaveType)i);
                        if (LeaveBalance == null)
                        {
                            LeaveBalance = new LeaveBalance();
                            LeaveBalance.LeaveType = (_leaveType)i;
                            LeaveBalance.UserID = User.Identity.Name;
                            LeaveBalance.AvailableLeaveHours = 0;
                            contextDb.LeaveBalances.Add(LeaveBalance);
                        }
                        else
                        {
                            LeaveBalance.AvailableLeaveHours -= appliedLeaveTimes[i];
                            contextDb.Entry(LeaveBalance).State = EntityState.Modified;
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
        public ActionResult Casual(TimeSheetContainer model, FormCollection formCol)
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
                        model.TimeRecordForm.SumbitStatus = TimeRecordForm._sumbitstatus.submitted;
                        model.TimeRecordForm.SubmitTime = DateTime.Now;
                        model.TimeRecordForm.UserID = User.Identity.Name;
                        model.TimeRecordForm.Period = Convert.ToInt32(formCol["period"].ToString());
                        model.TimeRecordForm.Year = Convert.ToInt32(formCol["year"].ToString());
                        model.TimeRecordForm.ManagerID = formCol["manager"].ToString();

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
                        model.TimeRecordForm.SumbitStatus = TimeRecordForm._sumbitstatus.submitted;
                        model.TimeRecordForm.UserID = User.Identity.Name;
                        model.TimeRecordForm.Period = Convert.ToInt32(formCol["period"].ToString());
                        model.TimeRecordForm.Year = Convert.ToInt32(formCol["year"].ToString());
                        model.TimeRecordForm.ManagerID = formCol["manager"].ToString();

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
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }

            //send email to manager
            TimeRecordForm form = (from f in contextDb.TimeRecordForms
                                   where f.Period == model.TimeRecordForm.Period
                                   where f.Year == model.TimeRecordForm.Year
                                   where f.UserID == model.TimeRecordForm.UserID
                                   select f).FirstOrDefault();
            if (form != null)
            {
                Task.Run(() => EmailSetting.SendEmail(form.ManagerID, string.Empty, "TimesheetApplication", form.TimeRecordFormID.ToString()));
            }
            return RedirectToAction("Index");
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
    }
}
