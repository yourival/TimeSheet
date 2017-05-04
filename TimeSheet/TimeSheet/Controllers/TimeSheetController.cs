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
        private bool firstTimeLoaded = true;

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
                model.TimeRecordForm.Year = year;
                model.TimeRecordForm.Period = period;
                model.TimeRecordForm.UserID = User.Identity.Name;

                
                for (int i = 0; i < 14; i++)
                {
                    TimeRecord record = new TimeRecord(firstPayDay.AddDays(i));
                    Debug.WriteLine(record.StartTime);
                    record.UserID = User.Identity.Name;
                    model.TimeRecords.Add(record);
                }
            }
            else
            {
                firstTimeLoaded = false;

                model.TimeRecordForm = form;

                for (int i = 0; i < 14; i++)
                {
                    DateTime date = firstPayDay.AddDays(i);
                    var record = (from r in contextDB.TimeRecords
                                         where DbFunctions.TruncateTime(r.StartTime) == date
                                         where r.UserID == User.Identity.Name
                                         select r).FirstOrDefault();

                    model.TimeRecords.Add(record);

                    
                }
            }

            return model;
        }


        public ActionResult SaveTimeSheet(TimeSheetContainer model)
        {
            if (firstTimeLoaded == true)
            {
                firstTimeLoaded = false;
                try
                {
                    if (ModelState.IsValid)
                    {
                        for (int i = 0; i < model.TimeRecords.Count; i++)
                        {
                            contextDB.TimeRecords.Add(model.TimeRecords[i]);
                            Debug.WriteLine(model.TimeRecords[i].StartTime);
                        }
                        contextDB.TimeRecordForms.Add(model.TimeRecordForm);
                        contextDB.SaveChanges();
                    }
                    return RedirectToAction("Index");
                }
                catch
                {
                    return RedirectToAction("Index");
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
                            contextDB.Entry(model.TimeRecords[i]).State = EntityState.Modified;
                        }
                        contextDB.TimeRecordForms.Attach(model.TimeRecordForm);
                        contextDB.Entry(model.TimeRecordForm).State = EntityState.Modified;
                        contextDB.SaveChanges();
                    }
                    return RedirectToAction("Index");
                }
                catch
                {
                    return RedirectToAction("Index");
                }
            }
        }







        // GET: TimeSheet/Create
        public ActionResult Create(int year, int period)
        {
            DateTime firstPayDay = PayPeriod.GetStartDay(year, period);
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();
            for (int i = 0; i < 14; i++)
            {
                newTimeRecords.Add(new TimeRecord(firstPayDay.AddDays(i)));
            }
            return PartialView(@"~/Views/TimeSheet/_CreateTimeSheet.cshtml", newTimeRecords);
        }

        // POST: TimeSheet/Create
        [HttpPost]
        public ActionResult Create(List<TimeRecord> records)
        {
            try
            {
                foreach (TimeRecord record in records)
                {
                    contextDB.TimeRecords.Add(record);
                }
                contextDB.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: TimeSheet/Create
        public ActionResult CreateLeaveForm(DateTime start, DateTime end, _leaveType leaveType)
        {
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();
            for (int i = 0; i <= (end - start).Days; i++)
            {
                TimeRecord newTimeRecord = new TimeRecord(start.AddDays(i));

                //options for weekend and weekdays
                if ((int)start.AddDays(i).DayOfWeek == 6 || (int)start.AddDays(i).DayOfWeek == 0)
                {
                    newTimeRecord.LeaveTime = new TimeSpan(0, 0, 0);
                    newTimeRecord.leaveType = _leaveType.none;
                }
                else
                {
                    newTimeRecord.LeaveTime = new TimeSpan(7, 30, 0);
                    newTimeRecord.leaveType = leaveType;
                }
                newTimeRecords.Add(newTimeRecord);
            }
            return PartialView(newTimeRecords);
        }

        // POST: TimeSheet/Create
        [HttpPost]
        public ActionResult CreateLeaveForm(List<TimeRecord> records)
        {
            try
            {
                foreach (TimeRecord record in records)
                {
                    contextDB.TimeRecords.Add(record);
                }
                contextDB.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
