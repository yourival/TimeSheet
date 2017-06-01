using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
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
        public ActionResult ApprovalDetail(int id)
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
                foreach (var a in relatedApplications)
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
            //ViewBag.Type = type;

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

        public ActionResult Test()
        {
            return View();
        }
    }
}