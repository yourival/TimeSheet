using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class TimeSheetController : Controller
    {
        private TimeSheetDb contextDB = new TimeSheetDb();

        // GET: TimeSheet
        public async Task<ActionResult> Index()
        {
            ViewBag.Year = PayPeriod.GetYearItems();
            int year = DateTime.Now.Year;
            int period = (int)(DateTime.Now - PayPeriod.FirstPayDayOfYear(year)).Days / 14 + 2;
            TimeSheetContainer model = await this.GetTimeSheetModel(year, period);
            return View(model);
        }

        // GET: Year
        public ActionResult SelectDefaultYear()
        {
            ViewBag.Period = PayPeriod.GetPeriodItems(DateTime.Now.Year);
            return PartialView("SelectYear");
        }

        // POST: Year
        public ActionResult SelectYear(int year)
        {
            ViewBag.Period = PayPeriod.GetPeriodItems(year);
            return PartialView(year);
        }

        public async Task<ActionResult> GetTimeRecords(string text)
        {
            string[] words = text.Split('.');
            var y = int.Parse(words[0]);
            var p = int.Parse(words[1]);
            var model = await this.GetTimeSheetModel(y, p);
            return PartialView(@"~/Views/TimeSheet/_CreateTimeSheet.cshtml", model);
        }

        private async Task<TimeSheetContainer> GetTimeSheetModel(int year , int period )
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.TimeRecordForm = new TimeRecordForm();
            model.TimeRecords = new List<TimeRecord>();

            DateTime firstPayDay = PayPeriod.GetStartDay(year, period);

            var form = (from f in contextDB.TimeRecordForms
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
                var record = (from r in contextDB.TimeRecords
                              where r.RecordDate == date
                              where r.UserID == User.Identity.Name
                              select r).FirstOrDefault();
                PayPeriod.SetPublicHoliday(record);
                if (record == null)
                {
                    TimeRecord r = new TimeRecord(date);
                    r.UserID = User.Identity.Name;
                    PayPeriod.SetPublicHoliday(r);
                    model.TimeRecords.Add(r);
                }
                else
                {
                    model.TimeRecords.Add(record);
                }
            }

            return model;
        }


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
                            contextDB.TimeRecords.Add(model.TimeRecords[i]);
                            Debug.WriteLine(model.TimeRecords[i].StartTime);
                            Debug.WriteLine(model.TimeRecords[i].UserID);
                        }
                        contextDB.TimeRecordForms.Add(model.TimeRecordForm);
                        contextDB.SaveChanges();
                    }
                    return RedirectToAction("Index");
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
                            contextDB.TimeRecords.Attach(model.TimeRecords[i]);
                            var entry = contextDB.Entry(model.TimeRecords[i]);
                            entry.Property(e => e.StartTime).IsModified = true;
                            entry.Property(e => e.LunchBreak).IsModified = true;
                            entry.Property(e => e.EndTime).IsModified = true;
                            Debug.WriteLine(model.TimeRecords[i].StartTime);
                            contextDB.SaveChanges();
                        }
                        contextDB.TimeRecordForms.Attach(model.TimeRecordForm);
                        contextDB.Entry(model.TimeRecordForm).State = EntityState.Modified;
                        contextDB.SaveChanges();
                    }
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}
