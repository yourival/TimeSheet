using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class TimeSheetController : Controller
    {
        private TimeSheetDb contextDB = new TimeSheetDb();

        // GET: TimeSheet
        public ActionResult Index()
        {
            ViewBag.Year = PayPeriod.GetYearItems();
            return View();
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

        // GET: TimeSheet/Details/5
        public ActionResult Details(int id)
        {
            return View();
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

        // GET: TimeSheet/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: TimeSheet/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: TimeSheet/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: TimeSheet/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }
    }
}
