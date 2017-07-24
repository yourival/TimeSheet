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
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult Index()
        {
            return View();
        }

        [AuthorizeUser(Roles = "Manager")]
        // GET: Admin/UserLeaves
        public ActionResult UserLeaves()
        {
            List<string> model = timesheetDb.LeaveBalances.Select(l => l.UserID).Distinct().ToList();

            return View(model);
        }

        // GET: Admin/DisplayUserLeaves
        public ActionResult DisplayUserLeaves(string userId, int rowId)
        {
            return PartialView("_DisplayUserLeaves", GetBalanceVM(userId, rowId));
        }

        // GET: Admin/EditUserLeaves
        public ActionResult EditUserLeaves(string userId, int rowId)
        {
            return PartialView("_EditUserLeaves", GetBalanceVM(userId, rowId));
        }

        // POST: Admin/EditUserLeaves
        [HttpPost]
        public ActionResult EditUserLeaves(string userId, int rowId, double[] balances)
        {
            for (int i = 0; i < 3; i++)
            {
                var leaveBalance = timesheetDb.LeaveBalances.Find(userId, (_leaveType)i);
                if (leaveBalance == null)
                {
                    timesheetDb.LeaveBalances.Add(new LeaveBalance()
                    {
                        UserID = userId,
                        LeaveType = (_leaveType)i,
                        AvailableLeaveHours = balances[i]
                    });
                }
                else
                {
                    leaveBalance.AvailableLeaveHours = balances[i];
                    timesheetDb.Entry(leaveBalance).State = EntityState.Modified;
                }
                timesheetDb.SaveChanges();
            }

            return PartialView("_DisplayUserLeaves", GetBalanceVM(userId, rowId));
        }

        [AuthorizeUser(Roles = "Admin")]
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
            }

            holidayList = PayPeriod.GetHoliday();
            foreach (Holiday item in holidayList)
            {
                adminDb.Holidays.Add(item);
            }
            adminDb.SaveChanges();

            return RedirectToAction("Holidays");
        }

        [AuthorizeUser(Roles = "Admin")]
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

        [AuthorizeUser(Roles = "Admin")]
        public ActionResult UserRoleSetting()
        {
            List<UserRoleSetting> UserRoleList = adminDb.UserRoleSettings.ToList();
            return View(UserRoleList);
        }

        //Get CreateUserRole view
        public ActionResult CreateUserRole()
        {
            return View();
        }

        //Save UserRole Info to Db
        [HttpPost]
        public ActionResult CreateUserRole(UserRoleSetting model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (model != null)
                    {
                        adminDb.UserRoleSettings.Add(model);
                        adminDb.SaveChanges();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return RedirectToAction("UserRoleSetting");
        }

        //Get Edit UserRole view
        public ActionResult EditUserRole(int id)
        {
            UserRoleSetting model = adminDb.UserRoleSettings.Find(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        //Save UserRole info to Db
        [HttpPost]
        public ActionResult EditUserRoleConfirmed(UserRoleSetting model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    adminDb.UserRoleSettings.Attach(model);
                    adminDb.Entry(model).State = EntityState.Modified;
                    adminDb.SaveChanges();
                }
                return RedirectToAction("UserRoleSetting");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        //Delete a UserRole by ID
        public ActionResult DeleteUserRole(int id)
        {
            UserRoleSetting model = adminDb.UserRoleSettings.Find(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            adminDb.UserRoleSettings.Remove(model);
            adminDb.SaveChanges();
            return RedirectToAction("UserRoleSetting");
        }

        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult PayrollExport()
        {
            ViewBag.Year = PayPeriod.GetYearItems();
            return View();
        }

        public ActionResult SelectDefaultPeriod()
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems(DateTime.Now.Year);
            foreach (var item in model.PeriodList)
            {
                if (item.Selected == true)
                {
                    int period = Convert.ToInt32(item.Value);
                    PeriodDetails(DateTime.Now.Year, period);
                }
            }

            return PartialView("_SelectYear", model);
        }

        public ActionResult SelectPeriod(int year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems(year);
            return PartialView("_SelectYear", model);
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

        public async Task<FileContentResult> PayrollExportResult(int year, int period)
        {
            //update the ADUser from AD first before exporting the csv file
            await ADUser.GetADUser();

            
            DateTime StartDate = PayPeriod.GetStartDay(year, period);
            DateTime EndDate = PayPeriod.GetEndDay(year, period);

            List<Payroll> payrolls = (from t1 in timesheetDb.TimeRecords
                                      where t1.LeaveType != null
                                            && t1.RecordDate >= StartDate
                                            && t1.RecordDate <= EndDate
                                      group t1 by new { t1.UserID, t1.LeaveType } into t2
                                      join u in timesheetDb.ADUsers on t2.FirstOrDefault().UserID equals u.Email // Email used as UserID in TimeRecord table
                                      select new Payroll
                                      {
                                          UserName = u.UserName,
                                          JobCode = u.JobCode,
                                          EmployeeID = u.EmployeeID,
                                          RecordDate = t2.FirstOrDefault().RecordDate,
                                          Flexi = t2.FirstOrDefault().Flexi,
                                          LeaveTime = t2.Sum(t => t.LeaveTime),
                                          LeaveType = t2.FirstOrDefault().LeaveType
                                      }).ToList();

            DataTable dt = new DataTable();
            DataColumn LastName = new DataColumn("Employee Last Name", typeof(string));
            dt.Columns.Add(LastName);

            DataColumn FirstName = new DataColumn("Employee First Name", typeof(string));
            dt.Columns.Add(FirstName);

            DataColumn Job = new DataColumn("Job", typeof(string));
            dt.Columns.Add(Job);

            DataColumn Payroll = new DataColumn("Leave Type", typeof(string));
            dt.Columns.Add(Payroll);
            
            DataColumn Units = new DataColumn("Units", typeof(double));
            dt.Columns.Add(Units);

            DataColumn Notes = new DataColumn("Notes", typeof(string));
            dt.Columns.Add(Notes);

            DataColumn ID = new DataColumn("Employee Card ID", typeof(int));
            dt.Columns.Add(ID);

            if (payrolls != null)
            {
                for (int i = 0; i < payrolls.Count; i++)
                {
                    DataRow dr = dt.NewRow();

                    if(payrolls[i].UserName.Contains(' '))
                    {
                        string[] words = payrolls[i].UserName.Split(' ');
                        dr["Employee Last Name"] = words[1];
                        dr["Employee First Name"] = words[0];
                    }
                    else
                        dr["Employee Last Name"] = payrolls[i].UserName;

                    dr["Job"] = payrolls[i].JobCode;
                    dr["Employee Card ID"] = payrolls[i].EmployeeID;
                    dr["Notes"] = null;
                    
                    dr["Leave Type"] = payrolls[i].LeaveType.GetDisplayName();
                    dr["Units"] = payrolls[i].LeaveTime;
                    dt.Rows.Add(dr);
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

        private LeaveBalanceViewModel GetBalanceVM(string userId, int rowId)
        {
            List<LeaveBalance> leaveBalances = new List<LeaveBalance>();
            
            for (int i = 0; i < 3; i++)
            {
                var leaveBalance = timesheetDb.LeaveBalances.Find(userId, (_leaveType)i);
                if (leaveBalance == null)
                {
                    ADUser user = timesheetDb.ADUsers.Where(u => u.Email == userId).FirstOrDefault();
                    string userName = user.UserName ?? string.Empty;
                    leaveBalance = new LeaveBalance()
                    {
                        LeaveType = (_leaveType)i,
                        UserID = userId,
                        UserName = userName
                    };
                }
                leaveBalances.Add(leaveBalance);
            }

            LeaveBalanceViewModel model = new LeaveBalanceViewModel()
            {
                RowId = rowId,
                Balances = leaveBalances
            };
            return model;
        }

        //public async Task<FileContentResult> PayrollExportResult(string year, string period)
        //{
        //    update the ADUser from AD first before exporting the csv file
        //    await ADUser.GetADUser();

        //    int y = Convert.ToInt32(year);
        //    int p = Convert.ToInt32(period);
        //    DateTime StartDate = PayPeriod.GetStartDay(y, p);
        //    DateTime EndDate = PayPeriod.GetEndDay(y, p);

        //    List<Payroll> payrolls = (from t in timesheetDb.TimeRecords
        //                              join u in timesheetDb.ADUsers on t.UserID equals u.Email // Email used as UserID in TimeRecord table
        //                              where t.RecordDate >= StartDate && t.RecordDate <= EndDate
        //                              group by
        //                              select new Payroll
        //                              {
        //                                  UserName = u.UserName,
        //                                  JobCode = u.JobCode,
        //                                  EmployeeID = u.EmployeeID,
        //                                  IsHoliday = t.IsHoliday,
        //                                  RecordDate = t.RecordDate,
        //                                  StartTime = t.StartTime,
        //                                  EndTime = t.EndTime,
        //                                  LunchBreak = t.LunchBreak,
        //                                  Flexi = t.Flexi,
        //                                  LeaveTime = t.LeaveTime,
        //                                  LeaveType = t.LeaveType
        //                              }).ToList();

        //    DataTable dt = new DataTable();
        //    DataColumn LastName = new DataColumn("Employee Last Name", typeof(string));
        //    dt.Columns.Add(LastName);

        //    DataColumn FirstName = new DataColumn("Employee First Name", typeof(string));
        //    dt.Columns.Add(FirstName);

        //    DataColumn Payroll = new DataColumn("Payroll Category", typeof(string));
        //    dt.Columns.Add(Payroll);

        //    DataColumn Job = new DataColumn("Job", typeof(string));
        //    dt.Columns.Add(Job);

        //    DataColumn Notes = new DataColumn("Notes", typeof(string));
        //    dt.Columns.Add(Notes);

        //    DataColumn date = new DataColumn("Date", typeof(string));
        //    dt.Columns.Add(date);

        //    DataColumn Units = new DataColumn("Units", typeof(double));
        //    dt.Columns.Add(Units);

        //    DataColumn ID = new DataColumn("Employee Card ID", typeof(int));
        //    dt.Columns.Add(ID);

        //    if (payrolls != null)
        //    {
        //        for (int i = 0; i < payrolls.Count; i++)
        //        {
        //            string[] words = payrolls[i].UserName.Split(' ');

        //            DataRow dr = dt.NewRow();
        //            dr["Employee Last Name"] = words[1];
        //            dr["Employee First Name"] = words[0];

        //            dr["Job"] = payrolls[i].JobCode;
        //            dr["Employee Card ID"] = payrolls[i].EmployeeID;
        //            dr["Notes"] = null;

        //            dr["Date"] = payrolls[i].RecordDate.ToString("dd/MM/yyyy");
        //            dr["Units"] = payrolls[i].WorkHours * (payrolls[i].Flexi ? 1.5 : 1);

        //            if (payrolls[i].IsHoliday)
        //            {
        //                if (payrolls[i].RecordDate.DayOfWeek == DayOfWeek.Saturday ||
        //                    payrolls[i].RecordDate.DayOfWeek == DayOfWeek.Sunday)
        //                    dr["Payroll Category"] = "Holiday Pay";
        //                else
        //                    dr["Payroll Category"] = "Public Holiday Pay";
        //            }
        //            else
        //            {
        //                dr["Payroll Category"] = "Base Hourly";
        //            }
        //            dt.Rows.Add(dr);

        //            if (payrolls[i].LeaveTime != 0 && payrolls[i].LeaveType != null)
        //            {
        //                DataRow drw = dt.NewRow();
        //                drw["Employee Last Name"] = words[1];
        //                drw["Employee First Name"] = words[0];

        //                drw["Job"] = payrolls[i].JobCode;
        //                drw["Employee Card ID"] = payrolls[i].EmployeeID;
        //                drw["Notes"] = null;

        //                drw["Date"] = payrolls[i].RecordDate.ToString("dd/MM/yyyy");
        //                drw["Units"] = payrolls[i].LeaveTime;
        //                drw["Payroll Category"] = payrolls[i].LeaveType.GetDisplayName();

        //                dt.Rows.Add(drw);
        //            }
        //        }
        //    }

        //    return GetCSV(dt);
        //}
    }
}
