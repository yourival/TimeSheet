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

        private List<TimeRecordForm> GetFormList(string type)
        {
            List<TimeRecordForm> formList = new List<TimeRecordForm>();
            if (type == "Waiting")
            {
                formList = (from f in contextDb.TimeRecordForms
                            where f.ManagerID == User.Identity.Name
                            where f.SumbitStatus == TimeRecordForm._sumbitstatus.submitted
                            where f.FormStatus == TimeRecordForm._formstatus.modified
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
                ViewBag.PeriodBegin = string.Format("0:dd/mm/yyyy",PayPeriod.GetStartDay(form.Year, form.Period));
                ViewBag.PeriodEnd = string.Format("0:dd/mm/yyyy", PayPeriod.GetEndDay(form.Year, form.Period));
                ViewBag.RequestedHours = 7.5 * 14;
                return View(form);
            }
            else
            {
                return HttpNotFound("Cannot find the application in database. Please contact our IT support.");
            }
        }

        [HttpPost]
        public ActionResult ApprovalDetail (int id, string decision)
        {

        }
    }
}