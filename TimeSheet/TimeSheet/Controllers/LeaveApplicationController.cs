using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class LeaveApplicationController : Controller
    {
        private TimeSheetDb contextDB = new TimeSheetDb();

        // GET: LeaveApplication
        public ActionResult Index()
        {
            return View();
        }

        // GET: LeaveApplication/Details/5
        public ActionResult Details(int id)
        {
            return View(contextDB.LeaveApplications);
        }

        // GET: LeaveApplication/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: LeaveApplication/Create
        [HttpPost]
        public ActionResult Create(LeaveApplication newLeaveApplication)
        {
            try
            {
                contextDB.LeaveApplications.Add(newLeaveApplication);
                contextDB.SaveChanges();

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: LeaveApplication/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: LeaveApplication/Edit/5
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

        // GET: LeaveApplication/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: LeaveApplication/Delete/5
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

        // GET: LeaveApplication/CreateLeaveForm
        public ActionResult CreateLeaveForm(DateTime start, DateTime end, _leaveType leaveType)
        {
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();
            for (int i = 0; i <= (end - start).Days; i++)
            {
                TimeRecord newTimeRecord = new TimeRecord(start.AddDays(i));
                newTimeRecord.LeaveType = leaveType;
                newTimeRecords.Add(newTimeRecord);
            }
            PayPeriod.SetPublicHoliday(newTimeRecords);

            return PartialView(newTimeRecords);
        }

        // POST: LeaveApplication/CreateLeaveForm
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
