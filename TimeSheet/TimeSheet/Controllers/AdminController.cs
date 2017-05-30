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
using System.Data;

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
                var leaveRecord = timesheetDb.LeaveRecords.Find(leaveRecords.First().UserID, (_leaveType)i);
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
                adminDb.EmailSetting.Add(model);
                adminDb.SaveChanges();
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

        public ActionResult TimesheetExport()
        {
            ViewBag.Year = PayPeriod.GetYearItems();
            return View();
        }

        public ActionResult SelectDefaultPeriod()
        {
            ViewBag.Period = PayPeriod.GetPeriodItems(DateTime.Now.Year);
            return PartialView("_SelectPeriod");
        }

        public ActionResult SelectPeriod(int year)
        {
            ViewBag.Period = PayPeriod.GetPeriodItems(year);
            return PartialView("_SelectPeriod");
        }
        
        public FileContentResult TimesheetExport(string year, string period)
        {
            int y = Convert.ToInt32(year);
            int p = Convert.ToInt32(period);
            DateTime StartDate = PayPeriod.GetStartDay(y, p);
            DateTime EndDate = PayPeriod.GetEndDay(y, p);

            List<Payroll> payrolls = (from t in timesheetDb.TimeRecords
                                      join u in timesheetDb.UserInfo on t.UserID equals u.Email // Email used as UserID in TimeRecord table
                                      where t.RecordDate >= StartDate && t.RecordDate <= EndDate
                                      select new Payroll
                                      {
                                          UserName = u.UserName,
                                          JobCode = u.JobCode,
                                          EmployeeID = u.EmployeeID,
                                          IsHoliday = t.IsHoliday,
                                          RecordDate = t.RecordDate,
                                          StartTime = t.StartTime,
                                          EndTime = t.EndTime,
                                          LunchBreak = t.LunchBreak,
                                          Flexi = t.Flexi,
                                          LeaveTime = t.LeaveTime,
                                          LeaveType = t.LeaveType
                                      }).ToList();

            DataTable dt = new DataTable();
            DataColumn LastName = new DataColumn("Employee Last Name", typeof(string));
            dt.Columns.Add(LastName);

            DataColumn FirstName = new DataColumn("Employee First Name", typeof(string));
            dt.Columns.Add(FirstName);

            DataColumn Payroll = new DataColumn("Payroll Category", typeof(string));
            dt.Columns.Add(Payroll);

            DataColumn Job = new DataColumn("Job", typeof(string));
            dt.Columns.Add(Job);

            DataColumn Notes = new DataColumn("Notes",typeof(string));
            dt.Columns.Add(Notes);

            DataColumn date = new DataColumn("Date", typeof(string));
            dt.Columns.Add(date);

            DataColumn Units = new DataColumn("Units", typeof(int));
            dt.Columns.Add(Units);

            DataColumn ID = new DataColumn("Employee Card ID", typeof(int));
            dt.Columns.Add(ID);

            if(payrolls != null)
            {
                for(int i=0; i < payrolls.Count; i++)
                {
                    string[] words = payrolls[i].UserName.Split(',');

                    DataRow dr = dt.NewRow();
                    dr["Employee Last Name"] = words[0];
                    dr["Employee First Name"] = words[1];

                    dr["Job"] = payrolls[i].JobCode;
                    dr["Employee Card ID"] = payrolls[i].EmployeeID;
                    dr["Notes"] = null;

                    dr["Date"] = string.Format("0:dd/mm/yyyy", payrolls[i].RecordDate);
                    dr["Units"] = payrolls[i].Flexi ? payrolls[i].GetWorkHours() * 1.5 : payrolls[i].GetWorkHours();

                    if (payrolls[i].IsHoliday)
                    {
                        if(payrolls[i].RecordDate.DayOfWeek == DayOfWeek.Saturday ||
                            payrolls[i].RecordDate.DayOfWeek == DayOfWeek.Sunday)
                            dr["Payroll Category"] = "Holiday Pay";
                        else
                            dr["Payroll Category"] = "Public Holiday Pay";
                    }
                    else
                    {
                        dr["Payroll Category"] = "Base Hourly";
                    }
                    dt.Rows.Add(dr);

                    if(payrolls[i].LeaveTime != 0 && payrolls[i].LeaveType != _leaveType.none)
                    {
                        DataRow drw = dt.NewRow();
                        drw["Employee Last Name"] = words[0];
                        drw["Employee First Name"] = words[1];

                        drw["Job"] = payrolls[i].JobCode;
                        drw["Employee Card ID"] = payrolls[i].EmployeeID;
                        drw["Notes"] = null;

                        drw["Date"] = string.Format("0:dd/mm/yyyy", payrolls[i].RecordDate);
                        drw["Units"] = payrolls[i].LeaveTime;

                        switch (payrolls[i].LeaveType)
                        {
                            case _leaveType.annual:
                                drw["Payroll Category"] = "Holiday Pay";
                                break;
                            case _leaveType.flexi:
                                drw["Payroll Category"] = "Flexi Pay";
                                break;
                            case _leaveType.sick:
                                drw["Payroll Category"] = "Sick Pay";
                                break;
                        }
                        dt.Rows.Add(drw);
                    }
                }
            }

            //conver to csv file continues from here

            return View();
        }


    }
}
