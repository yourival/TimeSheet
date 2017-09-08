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
    /// <summary>
    ///     A controller processing users' submission of HR applications and casual work hours.
    /// </summary>
    [Authorize]
    public class HRApplicationController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();
        /// <summary>
        ///     Indicates status of a submission of a HR application
        /// </summary>
        public enum postRequestStatus {
            /// <summary>
            ///     Successful submission.
            /// </summary>
            success,
            /// <summary>
            ///     Failed submission.
            /// </summary>
            fail
        };

        /// <summary>
        ///     Create a page for user to fill and submit a HR application or casual work hours.
        /// </summary>
        /// <returns>A view that display different forms for different types of empolyees.</returns>
        // GET: LeaveApplication
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        ///     Create a tab page for user to fill and submit a HR application.
        /// </summary>
        /// <returns>A partial view with details of an application.</returns>
        public ActionResult Leave()
        {
            LeaveApplicationViewModel model = new LeaveApplicationViewModel();
            List<LeaveBalance> LeaveBalances = new List<LeaveBalance>();
            //get manager dropdown list
            ViewBag.Manager = UserRoleSetting.GetManagerItems();

            for (int i = 0; i < 3; i++)
            {
                var availableLeave = contextDb.LeaveBalances.Find(User.Identity.Name, (_leaveType)i);
                LeaveBalances.Add(availableLeave == null ? new LeaveBalance() : availableLeave);
            }
            model.LeaveBalances = LeaveBalances;

            return PartialView("_Leave", model);
        }

        /// <summary>
        ///     Create a tab page for user to apply casual work hours.
        /// </summary>
        /// <returns>A partial view with details of an application of casual work hours.</returns>
        public ActionResult Casual()
        {
            TimeSheetContainer model = new TimeSheetContainer();

            //get droplists of year and managers
            model.YearList = PayPeriod.GetYearItems();
            ViewBag.Manager = UserRoleSetting.GetManagerItems();

            return PartialView("_Casual", model);
        }

        /// <summary>
        ///     Create a page for a user to view personal application histories.
        /// </summary>
        /// <returns>A page of a list of personal application histories.</returns>
        // GET: LeaveApplication/ApplicationHistory
        public ActionResult ApplicationHistory()
        {
            List<LeaveApplication> applications = contextDb.LeaveApplications
                                                           .Where(a => a.UserID == User.Identity.Name)
                                                           .OrderByDescending(a => a.id).ToList();
            return View(applications);
        }

        /// <summary>
        ///     Create a page for a user to view details of a submitted application.
        /// </summary>
        /// <param name="id">The identifier of the <see cref="LeaveApplication"/>.</param>
        /// <returns>A view with details of an application.</returns>
        // GET: LeaveApplication/ApplicationDetail
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

                // Get manager names for dropdown list
                List<string> managerNames = new List<string>();
                foreach(var managerId in application._managerIDs)
                {
                    managerNames.Add(contextDb.ADUsers.Find(managerId).UserName);
                }
                ViewBag.Managers = managerNames;

                if(application.ApprovedTime != null)
                {
                    // Get leave balance
                    List<LeaveBalance> LeaveBalances = new List<LeaveBalance>();
                    if (application.OriginalBalances != null)
                        ViewBag.OriginalBalances = application.OriginalBalances.Split('/');
                    else
                        ViewBag.OriginalBalances = new string[] { "", "", "" };

                    if (application.CloseBalances != null)
                        ViewBag.CloseBalances = application.CloseBalances.Split('/');
                    else
                        ViewBag.CloseBalances = new string[] { "", "", "" };

                    // Get the manager who signed, if it is singed
                    if (application.ApprovedBy != null)
                        ViewBag.SignedManager = contextDb.ADUsers.Find(application.ApprovedBy).UserName;
                    else
                        ViewBag.SignedManager = string.Empty;
                }
                return View(model);
            }
        }

        /// <summary>
        ///     Dispay a list of pay periods when a year is selected.
        ///     The year is set to the current year by default.
        /// </summary>
        /// <param name="year">The selected year.</param>
        /// <returns>A dropdown list of pay periods in the selected year.</returns>
        public ActionResult SelectYear(int? year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems((year == null) ? DateTime.Now.Year : year.Value);
            return PartialView("_SelectYear", model);
        }

        /// <summary>
        ///     Once all form data are validated, the method will create/update <see cref="LeaveApplication"/>
        ///     and <see cref="TimeRecord"/> in the database, and then update the user's <see cref="LeaveBalance"/>
        ///     accordingly. After all the operations completed successfully, the system will send emails to
        ///     managers specified in the application.
        ///     
        ///     If any error happened, changes will be safely aborted and have no effects on database.
        /// </summary>
        /// <param name="applicationVM">The view model combine with <see cref="LeaveApplication"/>,
        ///     <see cref="LeaveBalance"/>, and <see cref="TimeRecord"/>.
        /// </param>
        /// <returns>A response view with messages of success or failure of the application.</returns>
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
                        // Configure attendance of a TimeRecord
                        if (r.LeaveType == _leaveType.flexiHours ||
                            r.LeaveType == _leaveType.additionalHours)
                        {
                            if(r.LeaveTime < 14.5)
                                r.SetAttendence(9, 9.5 + r.LeaveTime, 0.5);
                            else
                                r.SetAttendence(12 - r.LeaveTime/2.0, 12 + r.LeaveTime/2.0, 0);
                        }
                        else
                        {
                            if(r.LeaveTime <= 7.5)
                                r.SetAttendence(9, 17 - r.LeaveTime, 0.5);
                        }

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
                            if(timeRecord.LeaveType != null)
                            {
                                // Compassionate pay will take Sick leaves balance
                                if (timeRecord.LeaveType == _leaveType.compassionatePay)
                                {
                                    appliedLeaveTimes[(int)_leaveType.sick] -= timeRecord.LeaveTime;
                                }
                                // Flexi hours will increase Flexi leave balance
                                else if (timeRecord.LeaveType == _leaveType.flexiHours)
                                {
                                    appliedLeaveTimes[(int)_leaveType.flexi] += timeRecord.LeaveTime;
                                }
                                // Subtract balance for Sick Leave, Flexi Leave, and Annual Leave
                                else if ((int)timeRecord.LeaveType < 3)
                                {
                                    appliedLeaveTimes[(int)timeRecord.LeaveType] -= timeRecord.LeaveTime;
                                }
                            }

                            timeRecord.LeaveTime = r.LeaveTime;
                            timeRecord.LeaveType = r.LeaveType;
                            contextDb.Entry(timeRecord).State = EntityState.Modified;
                        }
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
                        application.ManagerIDs = applicationVM.LeaveApplication.ManagerIDs;
                        application.Comment = applicationVM.LeaveApplication.Comment;
                        application.ApprovedBy = null;
                        application.ApprovedTime = null;
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
                        foreach(var mangerId in applicationModel._managerIDs)
                        {
                            Task.Run(() => EmailSetting.SendEmail(mangerId, string.Empty, "LeaveApplication", applicationModel.id.ToString()));
                        }
                    }

                    return RedirectToAction("PostRequest", new { status = postRequestStatus.success });
                }
                //TempData["ErrorModel"] = ModelState.Values;
                return RedirectToAction("PostRequest", new { status = postRequestStatus.fail });
            }
            catch (RetryLimitExceededException /* dex */)
            {
                //Log the error (uncomment dex variable name and add a line here to write a log.
                ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists see your system administrator.");
            }
            catch(Exception e)
            {
                throw e;
            }
            return RedirectToAction("PostRequest", new { status = postRequestStatus.fail });
        }

        /// <summary>
        ///     Once all form data are validated, the method will create/update <see cref="TimeRecordForm"/>
        ///     and <see cref="TimeRecord"/>s in the database to be approved by a manager.
        ///     After all the operations completed successfully, the system will send emails to
        ///     managers specified in the application of casual hours.
        ///     
        ///     If any error happened, changes will be safely aborted and have no effects on database.
        /// </summary>
        /// <param name="model">The view model combine with <see cref="TimeRecordForm"/> and <see cref="TimeRecord"/>.
        /// </param>
        /// <returns>A response view with messages of success or failure of the application.</returns>
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

                    // Add/Update TimeSheetForm data
                    var form = contextDb.TimeRecordForms.Where(t => t.UserID == userId &&
                                                           t.Year == model.TimeRecordForm.Year &&
                                                           t.Period == model.TimeRecordForm.Period).FirstOrDefault();
                    if (form == null)
                    {
                        model.TimeRecordForm.status = _status.submited;
                        model.TimeRecordForm.UserID = User.Identity.Name;
                        model.TimeRecordForm.UserName = contextDb.ADUsers.Find(User.Identity.Name).UserName;
                        contextDb.TimeRecordForms.Add(model.TimeRecordForm);
                    }
                    else
                    {
                        form.status = _status.modified;
                        form.TotalWorkingHours = model.TimeRecordForm.TotalWorkingHours;
                        form.ManagerIDs = model.TimeRecordForm.ManagerIDs;
                        form.Comments = model.TimeRecordForm.Comments;
                        contextDb.Entry(form).State = EntityState.Modified;
                    }
                    contextDb.SaveChanges();

                    // send email to manager
                    form = (from f in contextDb.TimeRecordForms
                            where f.Period == model.TimeRecordForm.Period
                            where f.Year == model.TimeRecordForm.Year
                            where f.UserID == model.TimeRecordForm.UserID
                            select f).FirstOrDefault();

                    if (form != null)
                    {
                        foreach (var mangerId in form._managerIDs)
                        {
                            Task.Run(() => EmailSetting.SendEmail(mangerId, string.Empty, "TimesheetApplication", form.TimeRecordFormId.ToString()));
                        }
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

        /// <summary>
        ///     Create a list of <see cref="TimeRecord"/> for the applicant to edit details
        ///     on each day of an HR application.
        /// </summary>
        /// <param name="start">The start day of the HR application</param>
        /// <param name="end">The end day of the HR application</param>
        /// <param name="leaveType">The main leave type of the application. It is also the default
        ///     leave type of each <see cref="TimeRecord"/> in the period.</param>
        /// <returns>A partial view with a list of <see cref="TimeRecord"/> to edit leave details.</returns>
        public ActionResult CreateLeaveList(DateTime start, DateTime end, _leaveType leaveType)
        {
            // Create new Leaveapplication
            LeaveApplicationViewModel model = new LeaveApplicationViewModel();
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();
            //get leave type for holidays
            IEnumerable<SelectListItem> items = new List<SelectListItem>()
            {
                new SelectListItem
                {
                    Text = "Flexi Hours (earned)",
                    Value = _leaveType.flexiHours.ToString(),
                    Selected = true
                },
                new SelectListItem
                {
                    Text = "Additional Hours",
                    Value = _leaveType.additionalHours.ToString()
                }
            };
            ViewBag.HolidayLeaveTypeItems = items;

            for (int i = 0; i <= (end - start).Days; i++)
            {
                // Create new timerecords
                DateTime currentDate = start.AddDays(i);
                var newTimeRecord = new TimeRecord(currentDate.Date);
                PayPeriod.SetPublicHoliday(newTimeRecord);

                newTimeRecord.LeaveTime = (newTimeRecord.IsHoliday) ? 0 : 7.5;
                newTimeRecord.SetAttendence(null, null, 0);
                newTimeRecord.UserID = User.Identity.Name;
                newTimeRecord.LeaveType = leaveType;
                newTimeRecords.Add(newTimeRecord);
            }
            model.TimeRecords = newTimeRecords;

            return PartialView("_LeaveList", model);
        }

        /// <summary>
        ///     Create a list of <see cref="TimeRecord"/> for the applicant to edit details
        ///     on each day.
        /// </summary>
        /// <param name="year">The year of a pay period.</param>
        /// <param name="period">The pay period in a year.</param>
        /// <returns>A partial view with a list of <see cref="TimeRecord"/> to edit work hours.</returns>
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

        /// <summary>
        ///     Create a view to inform whether an application is submitted succesfully or not.
        /// </summary>
        /// <param name="status">Indicate the success/failure of submission.</param>
        /// <returns>A view to inform whether an application is submitted succesfully or not.</returns>
        public ActionResult PostRequest(postRequestStatus status)
        {
            switch (status)
            {
                case postRequestStatus.fail:
                    ViewBag.Title = "INVAILD APPLICATION";
                    ViewBag.Body = "Some data in the application are not valid.<br />" +
                                   "Please try again and check them before submitting.<br />" +
                                   "If the problem persists, please contact our IT support.";
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
