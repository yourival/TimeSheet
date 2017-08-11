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
using System.Diagnostics;

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
            return View();
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
                    foreach(var r in records)
                    {
                        string username = contextDb.ADUsers.Find(r.UserID).UserName;
                        approvalVM.TakenLeaves.Add(new Tuple<DateTime, string, double>
                        (
                            r.RecordDate,
                            username,
                            r.LeaveTime)
                        );
                    }
                }
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
            List<LeaveApplication> model = GetApplicationList(type);
            if (User.IsInRole("Manager") && !User.IsInRole("Admin") &&
                !(User.IsInRole("Accountant") && type == "Confirmed"))
                model = model.Where(a => a.ManagerID == User.Identity.Name).ToList();

            return PartialView("_" + type, model);
        }

        // GET: Admin/ApplicationList
        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult ApplicationList(string type)
        {
            List<LeaveApplication> model = GetApplicationList(type);
            if (User.IsInRole("Manager") && !User.IsInRole("Admin") &&
                !(User.IsInRole("Accountant") && type == "Confirmed"))
                model = model.Where(a => a.ManagerID == User.Identity.Name).ToList();

            return View(model);
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
                if (application.OriginalBalances != null)
                    ViewBag.OriginalBalances = application.OriginalBalances.Split('/');
                else
                    ViewBag.OriginalBalances = new string[] { "", "", "" };

                if (application.CloseBalances != null)
                    ViewBag.CloseBalances = application.CloseBalances.Split('/');
                else
                    ViewBag.CloseBalances = new string[] { "", "", "" };

                // Get manager name
                if(application.ApprovedBy != null)
                    ViewBag.ManagerName = contextDb.ADUsers.Find(application.ApprovedBy).UserName;
                else
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
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult DownloadAttachment(int id)
        {
            var fileToRetrieve = contextDb.Attachments.Find(id);
            return File(fileToRetrieve.Content, fileToRetrieve.ContentType);
        }

        // GET: /DownloadPdf/1
        //[ValidateInput(false)]
        //[AuthorizeUser(Roles = "Manager, Accountant")]
        //public ActionResult DownloadPdf(string html)
        //{
        //    Byte[] res = null;
        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        PdfDocument pdf = PdfGenerator.GeneratePdf(html, PageSize.A4);
        //        pdf.Save(ms);
        //        res = ms.ToArray();
        //    }
        //    return File(res, "application/pdf");
        //}

        // POST: Admin/Approval/ApplicationDetails/1
        [HttpPost]
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult ApprovalDetail(int id, string decision)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                if (!User.IsInRole("Admin") && User.Identity.Name != application.ManagerID)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                                                    "You are not authoried to approve the application");
                }
                else
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
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult ApprovalWaiting(int id, string decision)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                if (!User.IsInRole("Admin") && User.Identity.Name != application.ManagerID)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                                                    "You are not authoried to approve the application");
                }
                else
                    ApproveApplication(application, decision);
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return View("_Waiting", GetApplicationList("Waiting"));
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
        private void ApproveApplication(LeaveApplication application, string decision)
        {
            List<LeaveBalance> userLeaveBalances = (from l in contextDb.LeaveBalances
                                                    where l.UserID == application.UserID
                                                    select l).ToList();
            List<TimeRecord> appliedTimeRecords = application.GetTimeRecords();

            string[] originalBalances = application.OriginalBalances.Split('/');
            double[] takenLeaveTimes = new double[] { 0, 0, 0 };
            if (decision == "Approve")
            {
                string closeBalances = String.Empty;
                // Calculate taken leaves
                foreach (var timerecord in appliedTimeRecords)
                {
                    int index = (int)timerecord.LeaveType;
                    if(index < 3)
                        takenLeaveTimes[index] += timerecord.LeaveTime;
                    else if(timerecord.LeaveType == _leaveType.compassionatePay)
                        takenLeaveTimes[(int)_leaveType.sick] += timerecord.LeaveTime;
                    else if(timerecord.LeaveType == _leaveType.flexiHours)
                        takenLeaveTimes[(int)_leaveType.flexi] -= timerecord.LeaveTime;
                }
                // Record closed leave balances
                for (int i = 0; i < 3; i++)
                {
                    double originalBalance = double.Parse(originalBalances[i] ?? "0");
                    closeBalances += string.Format("{0:0.00}", originalBalance - takenLeaveTimes[i]);
                    if (i != 2)
                        closeBalances += "/";
                }
                application.CloseBalances = closeBalances;
                application.status = _status.approved;
            }
            else
            {
                application.status = _status.rejected;
                // Undo leave record in each time record
                foreach (var record in appliedTimeRecords)
                {
                    var entry = contextDb.TimeRecords.Find(record.id);
                    entry.LeaveTime = 0;
                    entry.LeaveType = null;
                    contextDb.Entry(entry).State = EntityState.Modified;
                }
                // Undo leave balances for the user
                for (int i = 0; i < 3; i++)
                {
                    LeaveBalance balance = contextDb.LeaveBalances.Find(application.UserID, (_leaveType)i);
                    balance.AvailableLeaveHours = double.Parse(originalBalances[i] ?? "0");
                }
                // Record closed leave balances
                application.CloseBalances = application.OriginalBalances;
            }
            // Record approval info
            application.ApprovedTime = DateTime.Now;
            application.ApprovedBy = User.Identity.Name;
            contextDb.Entry(application).State = EntityState.Modified;
            contextDb.SaveChanges();
        }
    }
}