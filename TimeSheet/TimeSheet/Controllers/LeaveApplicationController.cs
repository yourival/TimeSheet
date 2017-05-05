using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;
using System.Data.Entity;
using System.Diagnostics;

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
                // Try to fetch Leaveapplication from DB if it exists
                applicationVM.LeaveApplication.status = _status.submited;
                applicationVM.LeaveApplication.UserID = User.Identity.Name;
                var application = (from a in contextDB.LeaveApplications
                                                 where DbFunctions.TruncateTime(a.StartTime) == applicationVM.LeaveApplication.StartTime.Date &&
                                                       DbFunctions.TruncateTime(a.EndTime) == applicationVM.LeaveApplication.EndTime.Date &&
                                                       a.UserID == User.Identity.Name
                                                 select a).FirstOrDefault();

                // Update if exists, Add if not
                if (application == null)
                {
                    contextDB.LeaveApplications.Add(applicationVM.LeaveApplication);
                }
                else
                {
                    application.leaveType = applicationVM.LeaveApplication.leaveType;
                    application.ManagerID = applicationVM.LeaveApplication.ManagerID;
                    contextDB.Entry(application).State = EntityState.Modified;
                }
                contextDB.SaveChanges();

                foreach(var r in applicationVM.Records)
                {
                    // Try to fetch TimeRecord from DB if it exists
                    var record = (from a in contextDB.TimeRecords
                                         where DbFunctions.TruncateTime(a.StartTime) == r.StartTime.Date &&
                                                 a.UserID == r.UserID
                                         select a).FirstOrDefault();

                    // Update if exists, Add if not
                    if (record == null)
                    {
                        contextDB.TimeRecords.Add(r);
                    }
                    else
                    {
                        record.LeaveTime = r.LeaveTime;
                        record.LeaveType = r.LeaveType;
                        contextDB.Entry(record).State = EntityState.Modified;
                    }
                    contextDB.SaveChanges();
                }

                return RedirectToAction("Index");
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                Debug.WriteLine(e.StackTrace);
                return View();
            }
        }

        // GET: LeaveApplication/CreateLeaveForm
        public ActionResult CreateLeaveForm(DateTime start, DateTime end, _leaveType leaveType)
        {
            // Try to fetch Leaveapplication from DB if it exists
            LeaveApplicationViewModel newApplicationVM = new LeaveApplicationViewModel();
            List<TimeRecord> newTimeRecords = new List<TimeRecord>();

            for (int i = 0; i <= (end - start).Days; i++)
            {
                // Fetch each timerecord in DB if it exists
                DateTime currentDate = start.AddDays(i);
                var newTimeRecord = (from r in contextDB.TimeRecords
                                            where DbFunctions.TruncateTime(r.StartTime) == currentDate.Date &&
                                                  r.UserID == User.Identity.Name
                                            select r).FirstOrDefault();
                if (newTimeRecord == null)
                {
                    newTimeRecord = new TimeRecord(currentDate.Date);
                    newTimeRecord.UserID = User.Identity.Name;
                    newTimeRecord.LeaveType = leaveType;
                }
                newTimeRecords.Add(newTimeRecord);
            }
            PayPeriod.SetPublicHoliday(newTimeRecords);
            newApplicationVM.Records = newTimeRecords;

            return PartialView(newApplicationVM);
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
    }
}
