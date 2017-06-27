using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;
using System.Net;

namespace TimeSheet.Controllers
{
    [AuthorizeUser(Roles = "Manager")]
    public class LeaveApprovalController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        // GET: LeaveApproval
        public ActionResult Index()
        {
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
        public ActionResult ApprovalDetail(int? id)
        {
            if(id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ApprovalViewModel approvalVM = new ApprovalViewModel();

            // Get the appliction from DB
            var application = contextDb.LeaveApplications.Include(a => a.Attachments)
                                      .SingleOrDefault(a => a.id == id);
            if (application == null)
            {
                return HttpNotFound();
            }
            else
            {
                approvalVM.LeaveApplication = application;
                approvalVM.TimeRecords = application.GetTimeRecords();

                // Get related applications in the same periods
                List<LeaveApplication> relatedApplications = (from a in contextDb.LeaveApplications
                                                              where DbFunctions.TruncateTime(a.EndTime) >= application.StartTime.Date &&
                                                                    DbFunctions.TruncateTime(a.StartTime) <= application.EndTime.Date &&
                                                                       a.status == _status.approved &&
                                                                       a.UserID != application.UserID
                                                              select a).ToList();
                // Fetch each leave days in the same period and has been approved
                foreach (var a in relatedApplications)
                {
                    List<TimeRecord> records = (from r in contextDb.TimeRecords
                                                where DbFunctions.TruncateTime(r.RecordDate) >= application.StartTime.Date &&
                                                      DbFunctions.TruncateTime(r.RecordDate) <= application.EndTime.Date &&
                                                         r.UserID == a.UserID &&
                                                         r.LeaveType != _leaveType.none
                                                select r).ToList();
                    records.ForEach(r => approvalVM.TakenLeaves.Add(r));
                }
                approvalVM.TakenLeaves.Sort((x, y) => x.RecordDate.CompareTo(y.RecordDate));
                approvalVM.LeaveApplication = application;

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
                if (decision == "Approve")
                    application.status = _status.approved;
                else
                {
                    application.status = _status.rejected;
                    // Update leave balances
                    List<TimeRecord> rejectedTimeRecords = application.GetTimeRecords();
                    List<LeaveBalance> userLeaveBalances = (from l in contextDb.LeaveBalances
                                                            where l.UserID == application.UserID
                                                            select l).ToList();
                    foreach(var record in rejectedTimeRecords)
                    {
                        LeaveBalance leaveBalance = userLeaveBalances.First(l => l.LeaveType == record.LeaveType);
                        leaveBalance.AvailableLeaveHours += record.LeaveTime;
                        TimeRecord updatedRecord = new TimeRecord(record.RecordDate);
                        updatedRecord.UserID = application.UserID;
                        contextDb.Entry(updatedRecord).State = EntityState.Modified;
                        contextDb.Entry(leaveBalance).State = EntityState.Modified;
                    }
                }

                contextDb.Entry(application).State = EntityState.Modified;
                contextDb.SaveChanges();
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return RedirectToAction("Approval");
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
            ViewBag.Type = type;

            return PartialView(@"~/Views/LeaveApproval/_Approval.cshtml", applications);
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
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return View("_Approval", GetApplicationList(type));
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
            else if (type == "Confirmed")
            {
                applications = (from a in contextDb.LeaveApplications
                                where a.status == _status.rejected ||
                                         a.status == _status.approved
                                select a).OrderByDescending(a => a.id).ToList();
            }

            return applications;
        }

        // GET: /File/1
        public ActionResult Download(int id)
        {
            var fileToRetrieve = contextDb.Attachments.Find(id);
            return File(fileToRetrieve.Content, fileToRetrieve.ContentType);
        }
    }
}