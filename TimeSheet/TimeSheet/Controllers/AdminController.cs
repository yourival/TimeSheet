using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class AdminController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        // GET: Admin
        public ActionResult Index()
        {
            return View();
        }

        // GET: Admin/UserLeaves
        public ActionResult UserLeaves()
        {
            return View();
        }

        // GET: Admin/_UserLeaves
        public ActionResult CreateForm(string userId)
        {
            List<LeaveRecord> leaveRecords = new List<LeaveRecord>();
            for (int i = 1; i < 4; i++)
            {
                var leaveRecord = contextDb.LeaveRecords.Find(userId, (_leaveType)i);
                if(leaveRecord == null)
                {
                    leaveRecord = new LeaveRecord();
                    leaveRecord.LeaveType = (_leaveType)i;
                    leaveRecord.UserID = userId;
                }
                leaveRecords.Add(leaveRecord);
            }
            return PartialView(@"~/Views/Admin/_UserLeaves.cshtml", leaveRecords);
        }

        // GET: Admin/UserLeaves
        [HttpPost]
        public ActionResult UserLeaves(List<LeaveRecord> leaveRecords)
        {
            for (int i = 1; i < 4; i++)
            {
                var leaveRecord = contextDb.LeaveRecords.Find(User.Identity.Name, (_leaveType)i);
                if (leaveRecord == null)
                {
                    contextDb.LeaveRecords.Add(leaveRecords[i-1]);
                }
                else
                {
                    leaveRecord.AvailableLeaveTime = leaveRecords[i-1].AvailableLeaveTime;
                    contextDb.Entry(leaveRecord).State = EntityState.Modified;
                }
                contextDb.SaveChanges();
            }

            return View();
        }
    }
}