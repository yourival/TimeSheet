using System;
using System.Collections.Generic;
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
            TimeSheetContainer model = await this.GetTimeSheetModel(2017,1);
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

        public async Task<ActionResult> GetTimeRecords(string year, string period)
        {
            var y = int.Parse(year);
            var p = int.Parse(period);
            var model = await this.GetTimeSheetModel(y, p);
            return PartialView(@"~/Views/TimeSheet/_CreateTimeSheet.cshtml", model);
        }

        private async Task<TimeSheetContainer> GetTimeSheetModel(int year , int period )
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.TimeRecordForm = new TimeRecordForm();
            model.TimeRecords = new List<TimeRecord>();

            DateTime firstPayDay = PayPeriod.GetStartDay(year, period);

            TimeRecordForm form = (from f in contextDB.TimeRecordForm
                                 where f.Year == year
                                 where f.Period == period
                                 where f.UserID == User.Identity.Name
                                 select f) as TimeRecordForm;
            if (form == null)
            {
                model.TimeRecordForm.Year = year;
                model.TimeRecordForm.Period = period;
                model.TimeRecordForm.UserID = User.Identity.Name;

                
                for (int i = 0; i < 14; i++)
                {
                    model.TimeRecords.Add(new TimeRecord(firstPayDay.AddDays(i)));
                }
            }
            else
            {
                model.TimeRecordForm = form;
                for (int i = 0; i < 14; i++)
                {
                    TimeRecord record = (from r in contextDB.TimeRecords
                                         where r.StartTime.Date == firstPayDay.AddDays(i).Date
                                         where r.UserID == User.Identity.Name
                                         select r) as TimeRecord;

                    model.TimeRecords.Add(record);
                }

            }

            return model;
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
