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
using System.Text;
using System.Configuration;
using System.Net.Configuration;
using System.Web.Configuration;

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
                if (leaveRecord == null)
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
                    timesheetDb.LeaveRecords.Add(leaveRecords[i - 1]);
                }
                else
                {
                    leaveRecord.AvailableLeaveTime = leaveRecords[i - 1].AvailableLeaveTime;
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
            EmailSetting model = new EmailSetting();
            Configuration config = WebConfigurationManager.OpenWebConfiguration("~/");
            MailSettingsSectionGroup mailSettings = config.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;
            if (mailSettings != null)
            {
                model.FromEmail = mailSettings.Smtp.From;
                model.Password = mailSettings.Smtp.Network.Password;
                model.Username = mailSettings.Smtp.Network.UserName;
                model.SMTPHost = mailSettings.Smtp.Network.Host;
                model.SMTPPort = mailSettings.Smtp.Network.Port;
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
                    Configuration config = WebConfigurationManager.OpenWebConfiguration("~/");
                    MailSettingsSectionGroup mailSettings = config.GetSectionGroup("system.net/mailSettings") as MailSettingsSectionGroup;
                    if (model != null)
                    {
                        mailSettings.Smtp.From = model.FromEmail;
                        mailSettings.Smtp.Network.Password = model.Password;
                        mailSettings.Smtp.Network.UserName = model.Username;
                        mailSettings.Smtp.Network.Host = model.SMTPHost;
                        mailSettings.Smtp.Network.Port = model.SMTPPort;
                        config.Save();
                    }
                    
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
            for (int i = 0; i < managerList.Count(); i++)
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

        public ActionResult CSVExport()
        {
            ViewBag.Year = PayPeriod.GetYearItems();
            return View();
        }

        public ActionResult SelectDefaultPeriod()
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems(DateTime.Now.Year);
            foreach(var item in model.PeriodList)
            {
                if (item.Selected == true)
                {
                    int period = Convert.ToInt32(item.Value);
                    PeriodDetails(DateTime.Now.Year, period);
                }
            }

            return PartialView("_SelectPeriod",model);
        }

        public ActionResult SelectPeriod(int year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems(year);
            return PartialView("_SelectPeriod",model);
        }

        public ActionResult DefaultPeriodDetails()
        {
            int year = DateTime.Now.Year;
            foreach (var item in PayPeriod.GetPeriodItems(DateTime.Now.Year))
            {
                if (item.Selected == true)
                {
                    int period = Convert.ToInt32(item.Value);
                    ViewBag.PeriodBegin = PayPeriod.GetStartDay(year, period);
                    ViewBag.PeriodEnd = PayPeriod.GetEndDay(year, period);
                }
            }
            return PartialView("_PeriodDetails");
        }

        public ActionResult PeriodDetails(int year, int period)
        {
            ViewBag.PeriodBegin = PayPeriod.GetStartDay(year, period);
            ViewBag.PeriodEnd = PayPeriod.GetEndDay(year, period);
            return PartialView("_PeriodDetails");
        }

        public async Task<FileContentResult> CSVExportResult(string year, string period)
        {
            //update the ADUser from AD first before exporting the csv file
            await ADUser.GetADUser();

            int y = Convert.ToInt32(year);
            int p = Convert.ToInt32(period);
            DateTime StartDate = PayPeriod.GetStartDay(y, p);
            DateTime EndDate = PayPeriod.GetEndDay(y, p);

            List<Payroll> payrolls = (from t in timesheetDb.TimeRecords
                                      join u in timesheetDb.ADUsers on t.UserID equals u.Email // Email used as UserID in TimeRecord table
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

            DataColumn Notes = new DataColumn("Notes", typeof(string));
            dt.Columns.Add(Notes);

            DataColumn date = new DataColumn("Date", typeof(string));
            dt.Columns.Add(date);

            DataColumn Units = new DataColumn("Units", typeof(double));
            dt.Columns.Add(Units);

            DataColumn ID = new DataColumn("Employee Card ID", typeof(int));
            dt.Columns.Add(ID);

            if (payrolls != null)
            {
                for (int i = 0; i < payrolls.Count; i++)
                {
                    string[] words = payrolls[i].UserName.Split(' ');

                    DataRow dr = dt.NewRow();
                    dr["Employee Last Name"] = words[1];
                    dr["Employee First Name"] = words[0];

                    dr["Job"] = payrolls[i].JobCode;
                    dr["Employee Card ID"] = payrolls[i].EmployeeID;
                    dr["Notes"] = null;

                    dr["Date"] = payrolls[i].RecordDate.ToString("dd/MM/yyyy");
                    dr["Units"] = payrolls[i].WorkHours * (payrolls[i].Flexi ? 1.5 : 1);

                    if (payrolls[i].IsHoliday)
                    {
                        if (payrolls[i].RecordDate.DayOfWeek == DayOfWeek.Saturday ||
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

                    if (payrolls[i].LeaveTime != 0 && payrolls[i].LeaveType != _leaveType.none)
                    {
                        DataRow drw = dt.NewRow();
                        drw["Employee Last Name"] = words[0];
                        drw["Employee First Name"] = words[1];

                        drw["Job"] = payrolls[i].JobCode;
                        drw["Employee Card ID"] = payrolls[i].EmployeeID;
                        drw["Notes"] = null;

                        drw["Date"] = payrolls[i].RecordDate.ToString("dd/MM/yyyy");
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

            return GetCSV(dt);
        }

        public FileContentResult GetCSV(DataTable dt)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>().
                                              Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            return File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", "Payroll.csv");
        }

    }
}
