using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;
using System.Threading.Tasks;
using System.Net;

namespace TimeSheet.Controllers
{
    [AuthorizeUser(Roles = "Manager")]
    public class TimesheetApprovalController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();

        // GET: TimesheetApproval
        public ActionResult Approval()
        {
            return View();
        }

        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult ApprovalPartial(string type)
        {
            List<TimeRecordForm> model = GetFormList(type);
            // Managers can only view those timesheets sent to them
            if (User.IsInRole("Manager") && !User.IsInRole("Admin") && !User.IsInRole("Accountant"))
                model = model.Where(a => a.ManagerIDs.Contains(User.Identity.Name)).ToList();

            return PartialView("_" + type, model);
        }

        [HttpPost]
        public ActionResult ApprovalPartial (int id, string decision, string type)
        {
            TimeRecordForm form = contextDb.TimeRecordForms.Find(id);
            if (form != null)
            {
                if (decision == "Approved")
                    form.status = _status.approved;
                else
                    form.status = _status.rejected;
                contextDb.Entry(form).State = EntityState.Modified;
                contextDb.SaveChanges();
            }
            else
            {
                return HttpNotFound("Cannot find the timesheet in database. Please contact our IT support.");
            }

            return View("_Approval", GetFormList(type));
        }

        public ActionResult TimeSheetList(string type)
        {
            List<TimeRecordForm> formList = GetFormList(type);
            return View(formList);
        }


        // GET: TimesheetApproval/ApprovalDetail
        public ActionResult ApprovalDetail (string id)
        {
            int formID = Convert.ToInt32(id);
            TimeSheetContainer model = new TimeSheetContainer();
            TimeRecordForm form = contextDb.TimeRecordForms.Find(formID);
            if (form != null)
            {
                // Format period with dates
                DateTime start = PayPeriod.GetStartDay(form.Year, form.Period);
                DateTime end = PayPeriod.GetEndDay(form.Year, form.Period);
                ViewBag.Period = form.Period.ToString() + String.Format(" ({0:dd/MM} - {1:dd/MM})", start, end);

                // Get manager names
                List<string> managerNames = new List<string>();
                foreach(var managerId in form._managerIDs)
                {
                    managerNames.Add(contextDb.ADUsers.Find(managerId).UserName);
                }
                ViewBag.Managers = managerNames;

                // Get TimeRecords
                List<TimeRecord> timeRecords = (from t in contextDb.TimeRecords
                                                where t.UserID == form.UserID &&
                                                      t.RecordDate >= start && t.RecordDate <= end
                                                select t).ToList();
                
                model.TimeRecordForm = form;
                model.TimeRecords = timeRecords.Where(t => t.WorkHours != 0).ToList();
                return View(model);
            }
            else
            {
                return HttpNotFound("Cannot find the timesheet in database. Please contact our IT support.");
            }
        }
        // GET: TimesheetApproval/TimeSheetOutput
        public ActionResult TimeSheetOutput(int id)
        {
            TimeRecordForm form = contextDb.TimeRecordForms.Find(id);
            // Get manager name
            ViewBag.ManagerName = contextDb.ADUsers.Find(form.ApprovedBy).UserName ?? string.Empty;
            // Format period with dates
            DateTime start = PayPeriod.GetStartDay(form.Year, form.Period);
            DateTime end = PayPeriod.GetEndDay(form.Year, form.Period);
            ViewBag.Period = form.Period.ToString() + String.Format(" ({0:dd/MM} - {1:dd/MM})", start, end);
            return View(form);
        }

        [HttpPost]
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult ApprovalWaiting(int id, string decision)
        {
            TimeRecordForm form = contextDb.TimeRecordForms.Find(id);
            if (form != null)
            {
                if (!User.IsInRole("Admin") && !form.ManagerIDs.Contains(User.Identity.Name))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                                                    "You are not authoried to approve the timesheet");
                }
                else
                    ApproveTimeSheet(form, decision);
            }
            else
            {
                return HttpNotFound("Cannot find the timesheet in database. Please contact our IT support.");
            }

            return View("_Waiting", GetFormList("Waiting"));
        }

        private List<TimeRecordForm> GetFormList(string type)
        {
            List<TimeRecordForm> formList = new List<TimeRecordForm>();
            if (type == "Waiting")
            {
                formList = (from f in contextDb.TimeRecordForms
                            where f.status == _status.submited ||
                                    f.status == _status.modified
                            select f).OrderByDescending(f => f.TimeRecordFormId).ToList();
            }
            else if (type == "Confirmed")
            {
                formList = (from f in contextDb.TimeRecordForms
                            where f.status == _status.approved ||
                                    f.status == _status.rejected
                            select f).OrderByDescending(f => f.TimeRecordFormId).ToList();
            }
            return formList;
        }

        // POST: TimesheetApproval/ApprovalDetail
        [HttpPost]
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult ApprovalDetail(int id, string decision)
        {
            TimeRecordForm form = contextDb.TimeRecordForms.Find(id);
            if (form != null)
            {
                if (!User.IsInRole("Admin") && !form.ManagerIDs.Contains(User.Identity.Name))
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest,
                                                    "You are not authoried to approve the timesheet");
                }
                else
                    ApproveTimeSheet(form, decision);
            }
            else
            {
                return HttpNotFound("Cannot find the timesheet in database. Please contact our IT support.");
            }

            return RedirectToAction("Approval");
        }

        private ActionResult ApproveTimeSheet(TimeRecordForm form, string decision)
        {
            if (decision == "Approve")
                form.status = _status.approved;
            else if (decision == "Reject")
                form.status = _status.rejected;
            form.ApprovedBy = User.Identity.Name;
            form.ApprovedTime = DateTime.Now;
            contextDb.Entry(form).State = EntityState.Modified;
            contextDb.SaveChanges();
            Task.Run(() => EmailSetting.SendEmail(form.UserID, string.Empty, "TimesheetApproval", form.TimeRecordFormId.ToString()));
            return RedirectToAction("Approval");
        }
    }
}