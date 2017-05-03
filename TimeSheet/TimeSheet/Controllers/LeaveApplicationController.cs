using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;
using System.Data.Entity;

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
        public ActionResult Create(LeaveApplicationViewModel applicationVM)
        {
            try
            {
                LeaveApplication application = (from a in contextDB.LeaveApplications
                                                 where DbFunctions.TruncateTime(a.StartTime) == applicationVM.LeaveApplication.StartTime.Date &&
                                                       DbFunctions.TruncateTime(a.EndTime) == applicationVM.LeaveApplication.EndTime.Date &&
                                                       a.UserID == User.Identity.Name
                                                 select a) as LeaveApplication;
                if(application == null)
                {
                    contextDB.LeaveApplications.Add(applicationVM.LeaveApplication);
                }
                else
                {
                    contextDB.Entry(application).State = EntityState.Modified;
                }
                contextDB.SaveChanges();

                foreach(var r in applicationVM.Records)
                {
                    TimeRecord record = (from a in contextDB.TimeRecords
                                         where DbFunctions.TruncateTime(a.StartTime) == r.StartTime.Date &&
                                                 a.UserID == r.UserID
                                         select a) as TimeRecord;
                    if (record == null)
                    {
                        contextDB.TimeRecords.Add(r);
                    }
                    else
                    {
                        contextDB.Entry(record).CurrentValues.SetValues(r);
                    }
                    contextDB.SaveChanges();
                }

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
            // Try to fetch Leaveapplication from DB if it exists
            LeaveApplicationViewModel newApplicationVM = new LeaveApplicationViewModel();
            newApplicationVM.LeaveApplication = (from a in contextDB.LeaveApplications
                                                    where DbFunctions.TruncateTime(a.StartTime) == start.Date &&
                                                            DbFunctions.TruncateTime(a.EndTime) == end.Date &&
                                                            a.UserID == User.Identity.Name
                                                    select a) as LeaveApplication;
            List < TimeRecord > newTimeRecords = new List<TimeRecord>();
            
            // Initialise if it doesn't exist in DB
            if (newApplicationVM.LeaveApplication == null)
            {
                newApplicationVM.LeaveApplication = new LeaveApplication();
            }

            for (int i = 0; i <= (end - start).Days; i++)
            {
                // Fetch each timerecord in DB if it exists
                TimeRecord record = (from r in contextDB.TimeRecords
                                            where DbFunctions.TruncateTime(r.StartTime) == start.Date &&
                                                  r.UserID == User.Identity.Name
                                            select r).First() as TimeRecord;
                if (record == null)
                {
                    record = new TimeRecord(start.AddDays(i).Date);
                    record.LeaveType = leaveType;
                }
                newTimeRecords.Add(record);
            }
            PayPeriod.SetPublicHoliday(newTimeRecords);
            newApplicationVM.Records = newTimeRecords;

            return PartialView(newApplicationVM);
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
