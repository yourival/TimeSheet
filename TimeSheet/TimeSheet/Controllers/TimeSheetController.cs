using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.IO;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class TimeSheetController : Controller
    {
        private TimeSheetDb timesheetDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();

        // GET: TimeSheet
        public async Task<ActionResult> Index(int message = 0)
        {
            ViewBag.Manager = UserRoleSetting.GetManagerItems();
            int year = DateTime.Now.Year;
            int period = (int)(DateTime.Now - PayPeriod.FirstPayDayOfYear(year)).Days / 14 + 2;
            TimeSheetContainer model = await GetTimeSheetModel(year, period);
            model.YearList = PayPeriod.GetYearItems();
            switch (message)
            {
                case 0:
                    ViewBag.Message = "";
                    break;
                case 1:
                    ViewBag.Message = "Please save timesheet before submit";
                    break;
                case 2:
                    ViewBag.Message = "Timesheet approval email has been sent successfully";
                    break;
                case 3:
                    ViewBag.Message = "Timesheet has been saved successfully";
                    break;
                default:
                    ViewBag.Message = "no message";
                    break;
            }
            return View(model);
        }

        // GET: Year
        public ActionResult SelectDefaultYear()
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems(DateTime.Now.Year);
            return PartialView("_SelectYear",model);
        }

        // POST: Year
        public ActionResult SelectYear(int year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems(year);
            return PartialView("_SelectYear", model);
        }

        //Get year period user selected
        public async Task<ActionResult> GetTimeRecords(string text)
        {
            string[] words = text.Split('.');
            var y = int.Parse(words[0]);
            var p = int.Parse(words[1]);
            var model = await this.GetTimeSheetModel(y, p);
            return PartialView("_CreateTimeSheet", model);
        }

        //get time records based on year period 
        private async Task<TimeSheetContainer> GetTimeSheetModel(int year , int period )
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.TimeRecordForm = new TimeRecordForm();
            model.TimeRecords = new List<TimeRecord>();

            DateTime firstPayDay = PayPeriod.GetStartDay(year, period);

            var form = (from f in timesheetDb.TimeRecordForms
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
                var record = (from r in timesheetDb.TimeRecords
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

        [HttpPost]
        //save or update time records to db
        public ActionResult SaveTimeSheet(TimeSheetContainer model)
        {
            if (Startup.NoRecords == true)
            {
                try
                {
                    if (ModelState.IsValid)
                    {
                        for (int i = 0; i < model.TimeRecords.Count; i++)
                        {
                            timesheetDb.TimeRecords.Add(model.TimeRecords[i]);
                        }
                        model.TimeRecordForm.status = _status.modified;
                        model.TimeRecordForm.SubmittedTime = DateTime.Now;
                        model.TimeRecordForm.TotalWorkingHours = CalculateTotalWorkingHours(model);
                        model.TimeRecordForm.TotalLeaveHours = CalculateTotalLeaveHours(model);
                        timesheetDb.TimeRecordForms.Add(model.TimeRecordForm);
                        timesheetDb.SaveChanges();
                    }
                    return RedirectToAction("Index",new { message = 3 });
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
                            timesheetDb.TimeRecords.Attach(model.TimeRecords[i]);
                            var entry = timesheetDb.Entry(model.TimeRecords[i]);
                            entry.Property(e => e.StartTime).IsModified = true;
                            entry.Property(e => e.LunchBreak).IsModified = true;
                            entry.Property(e => e.EndTime).IsModified = true;
                            entry.Property(e => e.Flexi).IsModified = true;
                            timesheetDb.SaveChanges();
                        }
                        model.TimeRecordForm.status = _status.modified;
                        model.TimeRecordForm.TotalWorkingHours = CalculateTotalWorkingHours(model);
                        model.TimeRecordForm.TotalLeaveHours = CalculateTotalLeaveHours(model);
                        model.TimeRecordForm.SubmittedTime = DateTime.Now;
                        timesheetDb.TimeRecordForms.Attach(model.TimeRecordForm);
                        timesheetDb.Entry(model.TimeRecordForm).State = EntityState.Modified;
                        timesheetDb.SaveChanges();
                    }
                    return RedirectToAction("Index", new { message = 3 });
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }  
        }

        public ActionResult SendEmail(FormCollection form)
        {
            string managerID = form["manager"].ToString();
            int year = Convert.ToInt32(form["year"].ToString());
            int period = Convert.ToInt32(form["period"].ToString());
            
            var formModel = (from f in timesheetDb.TimeRecordForms
                        where f.Year == year
                        where f.Period == period
                        where f.UserID == User.Identity.Name
                        select f).FirstOrDefault();
            if (formModel == null)
            {
                return RedirectToAction("Index", new { message = 1 });
            }
            else
            {
                formModel.ManagerIDs = managerID;
                formModel.status = _status.submited;
                formModel.SubmittedTime = DateTime.Now;
                timesheetDb.TimeRecordForms.Attach(formModel);
                timesheetDb.Entry(formModel).State = EntityState.Modified;
                timesheetDb.SaveChanges();

                //do not wait for the async task to complete
                Task.Run(() => EmailSetting.SendEmail(managerID,string.Empty, "TimesheetApplication", formModel.TimeRecordFormId.ToString()));

                return RedirectToAction("Index", new { message = 2});
            }
        }

        //Calculate the total working hours to current date
        private double CalculateTotalWorkingHours(TimeSheetContainer model)
        {
            double workingHours = 0;
            DateTime current = DateTime.Now.Date;
            for(int i=0;i<=Convert.ToInt32((current - model.TimeRecords[0].RecordDate).TotalDays); i++)
            {
                workingHours += model.TimeRecords[i].WorkHours;
            }
            return workingHours;
        }

        //Calculate the total leaving hours to current date
        private double CalculateTotalLeaveHours(TimeSheetContainer model)
        {
            double leaveHours = 0;
            DateTime current = DateTime.Now.Date;
            for (int i = 0; i <= Convert.ToInt32((current - model.TimeRecords[0].RecordDate).TotalDays); i++)
            {
                leaveHours += model.TimeRecords[i].LeaveTime;
            }
            return leaveHours;
        }
    }
}
