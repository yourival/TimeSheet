using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using TimeSheet.Models;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace TimeSheet.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private TimeSheetDb timesheetDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();

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
                var leaveRecord = timesheetDb.LeaveRecords.Find(userId, (_leaveType)i);
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
                    timesheetDb.LeaveRecords.Add(leaveRecords[i-1]);
                }
                else
                {
                    leaveRecord.AvailableLeaveTime = leaveRecords[i-1].AvailableLeaveTime;
                    timesheetDb.Entry(leaveRecord).State = EntityState.Modified;
                }
                timesheetDb.SaveChanges();
            }

            return View();
        }

        //get holidays
        public ActionResult Holidays()
        {
            List<Holiday> holidayList = adminDb.Holidays.ToList();
            return View(holidayList);
        }

        //Update Holidays from government website
        public ActionResult UpdateHolidays()
        {
            List<Holiday> holidayList = adminDb.Holidays.ToList();
            if (holidayList.Count != 0)
            {
                foreach (Holiday item in holidayList)
                {
                    adminDb.Holidays.Remove(item);
                }
                adminDb.SaveChanges();
                holidayList = PayPeriod.GetHoliday();
                foreach (Holiday item in holidayList)
                {
                    adminDb.Holidays.Add(item);
                }
                adminDb.SaveChanges();
            }
            else
            {
                holidayList = PayPeriod.GetHoliday();
                foreach (Holiday item in holidayList)
                {
                    adminDb.Holidays.Add(item);
                }
                adminDb.SaveChanges();
            }

            return RedirectToAction("Holidays");
        }

        //Get Email setting from AdminDb
        public ActionResult EmailSetting()
        {
            EmailSetting model;
            model = adminDb.EmailSetting.ToList().FirstOrDefault();
            if (model == null)
            {
                model = new EmailSetting();
            }
            return View(model);
        }

        //Update Emailsetting 
        [HttpPost]
        public ActionResult UpdateEmailSetting(EmailSetting model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    adminDb.EmailSetting.Attach(model);
                    adminDb.Entry(model).State = EntityState.Modified;
                    adminDb.SaveChanges();
                }
                return RedirectToAction("EmailSetting");
            }
            catch (Exception ex)
            {
                throw ex;
            }   
        }

        public ActionResult ManagerSetting()
        {
            List<Manager> ManagerList = adminDb.ManagerSetting.ToList();
            return View(ManagerList);
        }

        //Get CreateManager view
        public ActionResult CreateManager()
        {
            return View();
        }

        //Save Manager Info to Db
        [HttpPost]
        public ActionResult CreateManager(Manager model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model != null)
                    {
                        string ID = model.ManagerID;
                        int id = model.id;
                        string name = model.ManagerName;
                        adminDb.ManagerSetting.Add(model);
                        adminDb.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return RedirectToAction("ManagerSetting");
        }

        //Get Edit Manager  view
        public ActionResult EditManager(int id)
        {
            Manager model = adminDb.ManagerSetting.Find(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        //Save Manager info to Db
        public ActionResult EditManagerConfirmed(Manager model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    adminDb.ManagerSetting.Attach(model);
                    adminDb.Entry(model).State = EntityState.Modified;
                    adminDb.SaveChanges();
                }
                return RedirectToAction("ManagerSetting");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Delete a Manager by ID
        public ActionResult DeleteManager(int id)
        {
            Manager model = adminDb.ManagerSetting.Find(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            adminDb.ManagerSetting.Remove(model);
            adminDb.SaveChanges();
            return RedirectToAction("ManagerSetting");
        }

        public static List<SelectListItem> GetManagerItems()
        {
            AdminDb adminDb = new AdminDb();
            List<SelectListItem> listItems = new List<SelectListItem>();
            List<Manager> managerList = adminDb.ManagerSetting.ToList();
            for(int i = 0; i < managerList.Count(); i++)
            {
                if (i == 0)
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = managerList[i].ManagerName,
                        Value = managerList[i].ManagerID,
                        Selected = true
                    });
                }
                else
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = managerList[i].ManagerName,
                        Value = managerList[i].ManagerID
                    });
                }
            }
            return listItems;
        }
    }
}
