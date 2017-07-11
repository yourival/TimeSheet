using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;
using System.Net;
using PdfSharp.Pdf;
using TheArtOfDev.HtmlRenderer.PdfSharp;
using System.IO;
using PdfSharp;

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
        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult Approval()
        {
            List<LeaveApplication> applications = contextDb.LeaveApplications.ToList();

            return View(applications);
        }

        // GET: Admin/Approval/1
        // GET: Admin/Approval/ApplicationDetails/1
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult ApprovalDetail(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            ApprovalViewModel approvalVM = new ApprovalViewModel();

            // Get the appliction from DB
            var application = contextDb.LeaveApplications
                                       .Include(a => a.Attachments)
                                       .SingleOrDefault(a => a.id == id);
            if (application == null)
            {
                return HttpNotFound("The application you request does not exist in our database. Please contact our IT support.");
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
                                                         r.LeaveType != null
                                                select r).ToList();
                    records.ForEach(r => approvalVM.TakenLeaves.Add(r));
                }
                approvalVM.TakenLeaves.Sort((x, y) => x.RecordDate.CompareTo(y.RecordDate));
                approvalVM.LeaveApplication = application;
                if (User.Identity.Name == application.ManagerID)
                    ViewBag.Authorized = true;
                else
                    ViewBag.Authorized = false;

                return View(approvalVM);
            }
        }

        // GET: Admin/ApprovalPartial
        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult ApprovalPartial(string type)
        {
            return PartialView("_" + type, GetApplicationList(type));
        }

        // GET: Admin/ApplicationList
        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult ApplicationList(string type)
        {
            return View(GetApplicationList(type));
        }

        // GET: Admin/ApplicationList
        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult ApplicationOutput(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                applicationVM.LeaveApplication = application;
                // Get leave balance
                List<LeaveBalance> LeaveBalances = new List<LeaveBalance>();
                ViewBag.OriginalBalances = application.OriginalBalances.Split('/') ?? new string[] {"","","" };
                ViewBag.CloseBalances = application.CloseBalances.Split('/') ?? new string[] { "", "", "" };

                // Get manager name
                ViewBag.ManagerName = contextDb.ADUsers.Find(application.ManagerID).UserName;
                // Get pay period
                int payPeriod1 = PayPeriod.GetPeriodNum(application.StartTime);
                int payPeriod2 = PayPeriod.GetPeriodNum(application.EndTime);
                string period = "Pay Period " + payPeriod1;
                if (payPeriod1 != payPeriod2)
                    period += " - " + payPeriod2;
                ViewBag.PayPeriod = period;

                return View(applicationVM);
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }


        }

        // GET: /DownloadAttachment/1
        public ActionResult DownloadAttachment(int id)
        {
            var fileToRetrieve = contextDb.Attachments.Find(id);
            return File(fileToRetrieve.Content, fileToRetrieve.ContentType);
        }

        // GET: /DownloadPdf/1
        //[ValidateInput(false)]
        public ActionResult DownloadPdf(string html)
        {
            Byte[] res = null;
            using (MemoryStream ms = new MemoryStream())
            {
                PdfDocument pdf = PdfGenerator.GeneratePdf("!!", PageSize.A4);
                pdf.Save(ms);
                res = ms.ToArray();
            }
            return File(res, "application/pdf");
        }

        // POST: Admin/Approval/ApplicationDetails/1
        [HttpPost]
        public ActionResult ApprovalDetail(int id, string decision)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                ApproveApplication(application, decision);
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return RedirectToAction("Approval");
        }

        // POST: Admin/ApprovalPartial
        [HttpPost]
        public ActionResult ApprovalWaiting(int id, string decision, string type)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                ApproveApplication(application, decision);
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return View("_Waiting", GetApplicationList(type));
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

        // Convinence function: Approve/Reject an application
        private void ApproveApplication(LeaveApplication application,string decision)
        {
            List<LeaveBalance> userLeaveBalances = (from l in contextDb.LeaveBalances
                                                    where l.UserID == application.UserID
                                                    select l).ToList();
            if (decision == "Approve")
            {
                string closeBalances = String.Empty;
                application.status = _status.approved;
                for(int i=0; i<3; i++)
                {
                    closeBalances += string.Format("{0:0.00}", userLeaveBalances.First(l => (int)l.LeaveType == i).AvailableLeaveHours);
                    if (i != 2)
                        closeBalances += "/";
                }
                application.CloseBalances = closeBalances;
            }
            else
            {
                application.status = _status.rejected;

                // Update leave balances
                List<TimeRecord> rejectedTimeRecords = application.GetTimeRecords();
                // Undo leave record in each time record
                foreach (var record in rejectedTimeRecords)
                {
                    LeaveBalance leaveBalance = userLeaveBalances.First(l => l.LeaveType == record.LeaveType);
                    leaveBalance.AvailableLeaveHours += record.LeaveTime;
                    record.LeaveTime = 0;
                    record.LeaveType = null;
                    var entry = contextDb.TimeRecords.Find(record.id);
                    contextDb.Entry(entry).State = EntityState.Modified;
                    contextDb.Entry(leaveBalance).State = EntityState.Modified;
                }
                application.CloseBalances = application.OriginalBalances;
            }
            application.ApprovedTime = DateTime.Now;
            contextDb.Entry(application).State = EntityState.Modified;
            contextDb.SaveChanges();
        }
    }
}