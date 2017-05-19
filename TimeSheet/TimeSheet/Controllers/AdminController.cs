using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    [Authorize]
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

        // POST: Admin/UserLeaves
        [HttpPost]
        public ActionResult UserLeaves(List<LeaveRecord> leaveRecords)
        {
            for (int i = 1; i < 4; i++)
            {
                var leaveRecord = contextDb.LeaveRecords.Find(leaveRecords.First().UserID, (_leaveType)i);
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

        // GET: Admin/Approval
        public ActionResult Approval()
        {
            List<LeaveApplication> applications = contextDb.LeaveApplications.ToList();

            return View(applications);
        }

        // GET: Admin/Approval/1
        // GET: Admin/Approval/ApplicationDetails/1
        public ActionResult ApprovalDetails(int id)
        {
            ApprovalViewModel approvalVM = new ApprovalViewModel();
            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();

            // Get the appliction from DB
            var application = contextDb.LeaveApplications.Find(id);
            if (application == null)
            {
                return HttpNotFound();
            }
            else
            {
                applicationVM.LeaveApplication = application;
                applicationVM.TimeRecords = application.GetTimeRecords();

                // Get related applications
                List<LeaveApplication> relatedApplications = (from a in contextDb.LeaveApplications
                                           where DbFunctions.TruncateTime(a.EndTime) >= application.StartTime.Date &&
                                                 DbFunctions.TruncateTime(a.StartTime) <= application.EndTime.Date &&
                                                    a.status == _status.approved &&
                                                    a.UserID != application.UserID
                                           select a).ToList();
                foreach(var a in relatedApplications)
                {
                    List<TimeRecord> records = (from r in contextDb.TimeRecords
                                                where DbFunctions.TruncateTime(r.RecordDate) >= application.StartTime.Date &&
                                                      DbFunctions.TruncateTime(r.RecordDate) <= application.EndTime.Date &&
                                                         r.UserID == a.UserID
                                                select r).ToList();
                    records.ForEach(r => approvalVM.TakenLeaves.Add(r));
                }
                approvalVM.TakenLeaves.Sort((x, y) => x.StartTime.CompareTo(y.StartTime));
                approvalVM.UserApplicationVM = applicationVM;

                return View(approvalVM);
            }         
        }

        // POST: Admin/Approval/ApplicationDetails/1
        [HttpPost]
        public ActionResult ApprovalDetail(int id, string decision)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                if (decision == "Approved")
                    application.status = _status.approved;
                else
                    application.status = _status.rejected;

                contextDb.Entry(application).State = EntityState.Modified;
                contextDb.SaveChanges();
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return RedirectToAction("ApprovalDetail", id);
        }


        // GET: Admin/Approval/ApplicationList
        public ActionResult ApplicationList(string type)
        {
            List<LeaveApplication> applications = GetApplicationList(type);
            return View(applications);
        }

        // GET: Admin/ApprovalPartial
        public ActionResult ApprovalPartial(string type)
        {
            List<LeaveApplication> applications = GetApplicationList(type);
            return View(applications);
        }

        // POST: Admin/ApprovalPartial
        [HttpPost]
        public ActionResult ApprovalPartial(int id, string decision, string type)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                if (decision == "Approved")
                    application.status = _status.approved;
                else
                    application.status = _status.rejected;

                contextDb.Entry(application).State = EntityState.Modified;
                contextDb.SaveChanges();
                ViewBag.Result = "Application is modified successfully!";
            }
            else
            {
                ViewBag.Result = "Something went wrong!";
            }

            return View("_Approval", GetApplicationList("Waiting"));
        }

        // Convinence function: Get appllications for a view
        private List<LeaveApplication> GetApplicationList(string type)
        {

            List<LeaveApplication> applications = new List<LeaveApplication>();
            if (type == "Waiting")
            {
                applications = (from a in contextDb.LeaveApplications
                                where a.status == _status.submited ||
                                         a.status == _status.modified
                                select a).OrderByDescending(a => a.id).ToList();
            }
            else
            {
                applications = (from a in contextDb.LeaveApplications
                                where a.status == _status.rejected ||
                                         a.status == _status.approved
                                select a).OrderByDescending(a => a.id).ToList();
            }
            return applications;
        }
    }
}
