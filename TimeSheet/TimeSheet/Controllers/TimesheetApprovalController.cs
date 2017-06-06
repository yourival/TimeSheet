using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class TimesheetApprovalController : Controller
    {
        private TimeSheetDb contextDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();

        // GET: TimesheetApproval
        public ActionResult Approval()
        {
            List<TimeRecordForm> formList = new List<TimeRecordForm>();
            formList = (from f in contextDb.TimeRecordForms
                        where f.ManagerID == User.Identity.Name
                        select f).OrderByDescending(f => f.TimeRecordFormID).ToList();
            return View(formList);
        }

        public ActionResult ApprovalPartial (string type)
        {
            List<TimeRecordForm> formList = GetFormList(type);

            return PartialView(@"~/Views/TimesheetApproval/_Approval.cshtml", formList);
        }

        [HttpPost]
        public ActionResult ApprovalPartial (int id, string decision, string type)
        {
            TimeRecordForm form = contextDb.TimeRecordForms.Find(id);
            if (form != null)
            {
                if (decision == "Approved")
                    form.FormStatus = TimeRecordForm._formstatus.approved;
                else
                    form.FormStatus = TimeRecordForm._formstatus.rejected;
                contextDb.Entry(form).State = EntityState.Modified;
                contextDb.SaveChanges();
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }

            return View("_Approval", GetFormList(type));
        }

        public ActionResult TimeSheetList(string type)
        {
            List<TimeRecordForm> formList = GetFormList(type);
            return View(formList);
        }

        private List<TimeRecordForm> GetFormList(string type)
        {
            List<TimeRecordForm> formList = new List<TimeRecordForm>();
            if (type == "Waiting")
            {
                formList = (from f in contextDb.TimeRecordForms
                            where f.ManagerID == User.Identity.Name
                            where f.SumbitStatus == TimeRecordForm._sumbitstatus.submitted
                            where f.FormStatus == TimeRecordForm._formstatus.modified ||
                                    f.FormStatus == TimeRecordForm._formstatus.submitted
                            select f).OrderByDescending(f => f.TimeRecordFormID).ToList();
            }
            else if (type == "Confirmed")
            {
                formList = (from f in contextDb.TimeRecordForms
                            where f.ManagerID == User.Identity.Name
                            where f.SumbitStatus == TimeRecordForm._sumbitstatus.submitted
                            where f.FormStatus == TimeRecordForm._formstatus.approved ||
                                    f.FormStatus == TimeRecordForm._formstatus.rejected
                            select f).OrderByDescending(f => f.TimeRecordFormID).ToList();
            }
            return formList;
        }

        public ActionResult ApprovalDetail (int id)
        {
            TimeRecordForm form = contextDb.TimeRecordForms.Find(id);
            if (form != null)
            {
                ViewBag.PeriodYear = string.Format("{0}/{1}", form.Period, form.Year);
                ViewBag.PeriodBegin = PayPeriod.GetStartDay(form.Year, form.Period);
                ViewBag.PeriodEnd = PayPeriod.GetEndDay(form.Year, form.Period);
                ViewBag.RequestedHours = 7.5 * 14;
                ViewBag.UserName = "Waiting for creating user table";
                Manager m = (from a in adminDb.ManagerSetting
                             where a.ManagerID == form.ManagerID
                             select a).FirstOrDefault();
                ViewBag.ManagerName = m.ManagerName;
                return View(form);
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }
        }

        public ActionResult ApprovalDetailPost (string id, string decision)
        {
            int formID = Convert.ToInt32(id);
            TimeRecordForm form = contextDb.TimeRecordForms.Find(formID);
            if(form != null)
            {
                if (decision == "Approved")
                    form.FormStatus = TimeRecordForm._formstatus.approved;
                if (decision == "Rejected")
                    form.FormStatus = TimeRecordForm._formstatus.rejected;
                contextDb.Entry(form).State = EntityState.Modified;
                contextDb.SaveChanges();
                return RedirectToAction("ApprovalDetail", new { id = formID });
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }
        }

        [HttpPost]
        public ActionResult LoadTimesheetDetail (int id)
        {
            TimeRecordForm form = contextDb.TimeRecordForms.Find(id);
            DateTime StartDate = PayPeriod.GetStartDay(form.Year, form.Period);
            DateTime EndDate = PayPeriod.GetEndDay(form.Year, form.Period);
            List<TimeRecord> TimeRecords = (from t in contextDb.TimeRecords
                                            where t.UserID == form.UserID
                                            where t.RecordDate >= StartDate && t.RecordDate <= EndDate
                                            select t).ToList();
            return PartialView("_TimesheetDetail",TimeRecords);
        }

        public ActionResult SaveComment(string id, string decision)
        {
            int formID = Convert.ToInt32(id);
            TimeRecordForm form = contextDb.TimeRecordForms.Find(formID);
            if (form != null)
            {

                form.Comments = decision;
                contextDb.Entry(form).State = EntityState.Modified;
                contextDb.SaveChanges();

                return RedirectToAction("ApprovalDetail", new { id = formID });
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }
        }
    }
}