using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace TimeSheet.Controllers
{
    /// <summary>
    ///     A controller proccessing approval of HR applications.
    /// </summary>
    public class LeaveApprovalController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();

        /// <summary>
        ///     Create a breif view of list of <see cref="LeaveApplication"/> and sorted into two groups:
        ///     applictions have been signed and those are not.
        /// </summary>
        /// <returns>A breif view of list of <see cref="LeaveApplication"/>.</returns>
        // GET: Admin/Approval
        [AuthorizeUser(Roles = "Admin, Manager, Accountant")]
        public ActionResult Approval()
        {
            return View();
        }

        /// <summary>
        ///     Create a view of an HR application for assigned managers to approve/reject.
        /// </summary>
        /// <param name="id">The identity of <see cref="LeaveApplication"/>.</param>
        /// <returns>A view of a HR application</returns>
        // GET: Admin/Approval/ApplicationDetails/1
        [AuthorizeUser(Roles = "Admin, Manager, Accountant")]
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
                if (application.ManagerIDs.Contains(User.Identity.Name))
                    ViewBag.Authorized = true;
                else
                    ViewBag.Authorized = false;

                return View(approvalVM);
            }
        }

        /// <summary>
        ///     Create a list of applications for either waiting or confirmed applications.
        ///     It displays up to 5 applications for a group in a page.
        /// </summary>
        /// <param name="type">"Waiting" means <c>_leaveType.submitted</c> and <c>_leaveType.modified</c>
        ///                    "Confirmed" means <c>_leaveType.approved</c> and <c>_leaveType.rejected</c></param>
        /// <returns>A view with a list of applications of either waiting or confirmed applications </returns>
        // GET: Admin/ApprovalPartial
        [AuthorizeUser(Roles = "Admin, Manager, Accountant")]
        public ActionResult ApprovalPartial(string type)
        {
            List<LeaveApplication> model = GetApplicationList(type);
            // Managers can only view those applications sent to them
            if (User.IsInRole("Manager") && !User.IsInRole("Admin") && !User.IsInRole("Accountant"))
                model = model.Where(a => a.ManagerIDs.Contains(User.Identity.Name)).ToList();

            return PartialView("_" + type, model);
        }

        /// <summary>
        ///     Create a list of applications for either waiting or confirmed applications.
        ///     It displays all applications in for a group in a page.
        /// </summary>
        /// <param name="type">"Waiting" means <c>_leaveType.submitted</c> and <c>_leaveType.modified</c>
        ///                    "Confirmed" means <c>_leaveType.approved</c> and <c>_leaveType.rejected</c></param>
        /// <returns>A view with a list of all applications of either waiting or confirmed applications \.</returns>
        // GET: Admin/ApplicationList
        [AuthorizeUser(Roles = "Admin, Manager, Accountant")]
        public ActionResult ApplicationList(string type)
        {
            List<LeaveApplication> model = GetApplicationList(type);
            // Managers can only view those applications sent to them
            if (User.IsInRole("Manager") && !User.IsInRole("Admin") && !User.IsInRole("Accountant"))
                model = model.Where(a => a.ManagerIDs.Contains(User.Identity.Name)).ToList();

            return View(model);
        }

        /// <summary>
        ///     Create a view to display details of a confirmed application and
        ///     allow a user to print it out easily.
        /// </summary>
        /// <param name="id">The identity of <see cref="LeaveApplication"/>.</param>
        /// <returns>A view with details of a confirmed application.</returns>
        // GET: Admin/ApplicationList
        [AuthorizeUser(Roles = "Admin, Manager, Accountant")]
        public ActionResult ApplicationOutput(int? id)
        {
            if (id == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            LeaveApplicationViewModel applicationVM = new LeaveApplicationViewModel();
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                applicationVM.LeaveApplication = application;
                applicationVM.TimeRecords = application.GetTimeRecords();
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
                ViewBag.ManagerName = contextDb.ADUsers.Find(application.ApprovedBy).UserName ?? string.Empty;

                // Get pay period and format it
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

        /// <summary>
        ///     Download an attachment when a manager is confirming an application.
        /// </summary>
        /// <param name="id">The identity of an attached file.</param>
        /// <returns>A file that is attahced.</returns>
        // GET: /DownloadAttachment/1
        [AuthorizeUser(Roles = "Admin, Manager")]
        public ActionResult DownloadAttachment(int id)
        {
            var fileToRetrieve = contextDb.Attachments.Find(id);
            return File(fileToRetrieve.Content, fileToRetrieve.ContentType);
        }

        /// <summary>
        ///     Process approval/rejection of an application via the detail page.
        /// </summary>
        /// <param name="id">The identity of the application</param>
        /// <param name="decision">The decision made by the manager, i.e. confirm or reject.</param>
        /// <returns>A breif view of list of <see cref="LeaveApplication"/>.</returns>
        // POST: Admin/Approval/ApplicationDetails/1
        [HttpPost]
        [AuthorizeUser(Roles = "Admin, Manager")]
        public async Task<ActionResult> ApprovalDetail(int id, string decision)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                if (!User.IsInRole("Admin") && !application.ManagerIDs.Contains(User.Identity.Name))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                                                    "You are not authoried to approve the application");
                }
                else
                    await ApproveApplication(application, decision);
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return RedirectToAction("Approval");
        }

        // POST: Admin/ApprovalPartial
        [HttpPost]
        [AuthorizeUser(Roles = "Admin, Manager")]
        public async Task<ActionResult> ApprovalWaiting(int id, string decision)
        {
            LeaveApplication application = contextDb.LeaveApplications.Find(id);
            if (application != null)
            {
                if (!User.IsInRole("Admin") && !application.ManagerIDs.Contains(User.Identity.Name))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                                                    "You are not authoried to approve the application");
                }
                else
                    await ApproveApplication(application, decision);
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
            if (User.IsInRole("Manager") && !User.IsInRole("Admin") && !User.IsInRole("Accountant"))
                applications = applications.Where(a => a.ManagerIDs.Contains(User.Identity.Name)).ToList();

            return applications;
        }

        // Convinence function: Approve/Reject an application
        private async Task ApproveApplication(LeaveApplication application, string decision)
        {
            if (application.status != _status.approved && application.status != _status.rejected)
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
                        if (index < 3)
                            takenLeaveTimes[index] += timerecord.LeaveTime;
                        else if (timerecord.LeaveType == _leaveType.compassionatePay)
                            takenLeaveTimes[(int)_leaveType.sick] += timerecord.LeaveTime;
                        else if (timerecord.LeaveType == _leaveType.flexiHours)
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

                // Send an email to manager
                try
                {
                    await Task.Run(() => EmailSetting.SendEmail(application.UserID, string.Empty, "LeaveApproval", application.id.ToString()));
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
}