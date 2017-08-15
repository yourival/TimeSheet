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
                        ViewBag.Message = "Successfully saved.";
                    }
                }
                return View("EmailSetting", model);
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

        // GET: Admin/PayrollExport
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

        // Export Leave Applications in a period
        public async Task<FileContentResult> CSVExport(string year, string period)
        {
            // update the ADUser from AD first before exporting the csv file
            await ADUser.GetADUser();

            DataTable dt = GetDataTable(year, period);
            return GetCSV(dt, year, period);
        }

        // GET: Admin/_Preview
        public ActionResult PayrollPreview(string year, string period)
        {
            DataTable dt = GetDataTable(year, period);
            return PartialView("_Preview", dt);
        }

        private DataTable GetDataTable(string year, string period)
        {
            int y = Convert.ToInt32(year);
            int p = Convert.ToInt32(period);
            DateTime startPeriod = PayPeriod.GetStartDay(y, p);
            DateTime endPeriod = PayPeriod.GetEndDay(y, p);
            ViewBag.Period = String.Format("{0:dd/MM/yy}", startPeriod) + " - " +
                             String.Format("{0:dd/MM/yy}", endPeriod);

            List<LeaveApplication> applications = (from a in timesheetDb.LeaveApplications
                                                  where !(a.EndTime < startPeriod ||
                                                          a.StartTime > endPeriod) &&
                                                         (a.status == _status.approved ||
                                                          a.status == _status.rejected)
                                                   select a).ToList();

            DataTable dt = new DataTable();

            DataColumn employeeId_col = new DataColumn("Employee Card ID", typeof(string));
            dt.Columns.Add(employeeId_col);

            DataColumn surnaame_col = new DataColumn("Surname", typeof(string));
            dt.Columns.Add(surnaame_col);

            DataColumn firstName_col = new DataColumn("First Name", typeof(string));
            dt.Columns.Add(firstName_col);

            DataColumn position_col = new DataColumn("Position", typeof(string));
            dt.Columns.Add(position_col);

            DataColumn date_col = new DataColumn("Date or Period", typeof(string));
            dt.Columns.Add(date_col);

            DataColumn totalHours_col = new DataColumn("Total Hours", typeof(double));
            dt.Columns.Add(totalHours_col);

            DataColumn type_col = new DataColumn("Leave Type / Additional Hours", typeof(string));
            dt.Columns.Add(type_col);

            DataColumn approvedBy_col = new DataColumn("Approved By", typeof(string));
            dt.Columns.Add(approvedBy_col);


            if (applications != null)
            {
                foreach (var application in applications)
                {
                    ADUser user = timesheetDb.ADUsers.Find(application.UserID);
                    string[] words = user.UserName.Split(' ');

                    List<TimeRecord> records = application.GetTimeRecords()
                                                .Where(r => r.RecordDate >= startPeriod &&
                                                            r.RecordDate <= endPeriod)
                                                .OrderBy(r => r.RecordDate).ToList();

                    // If the application is within the period and contains only 1 type
                    bool multiplyTypes = records.Any(r => r.LeaveType != application.leaveType);
                    if (application.StartTime >= startPeriod &&
                        application.EndTime <= endPeriod &&
                        !multiplyTypes)
                    {
                        DataRow dr = dt.NewRow();
                        dr["Employee Card ID"] = user.EmployeeID;
                        dr["Surname"] = words[1];
                        dr["First Name"] = words[0];
                        dr["Position"] = user.JobCode;
                        dr["Date or Period"] = String.Format("{0:dd/MM/yy}", application.StartTime);
                        if(application.StartTime != application.EndTime)
                            dr["Date or Period"] += " - " + String.Format("{0:dd/MM/yy}", application.EndTime);
                        dr["Total Hours"] = application.TotalLeaveTime;
                        dr["Leave Type / Additional Hours"] = application.leaveType.GetDisplayName();

                        string[] managerName = timesheetDb.ADUsers.Find(application.ApprovedBy).UserName.Split(' ');
                        dr["Approved By"] = managerName[1] + ", " + managerName[0];
                        dt.Rows.Add(dr);
                    }
                    else
                    {
                        _leaveType previousType = records.First().LeaveType.Value;
                        string startDate = String.Format("{0:dd/MM/yy}", records.First().RecordDate);
                        string endDate = string.Empty;
                        double totalHours = 0.0;

                        for (int i = 0; i < records.Count && records[i].RecordDate <= endPeriod; i++)
                        {
                            if (records[i].LeaveType == previousType)
                            {
                                totalHours += records[i].LeaveTime;
                                endDate = String.Format("{0:dd/MM/yy}", records[i].RecordDate);
                            }
                            if (records[i].LeaveType != previousType ||
                                i == records.Count - 1)
                            {
                                DataRow dr = dt.NewRow();
                                dr["Employee Card ID"] = user.EmployeeID;
                                dr["Surname"] = words[1];
                                dr["First Name"] = words[0];
                                dr["Position"] = user.JobCode;
                                dr["Leave Type / Additional Hours"] = previousType.GetDisplayName();
                                dr["Date or Period"] = startDate;
                                if (startDate != endDate)
                                    dr["Date or Period"] += " - " + endDate;
                                dr["Total Hours"] = totalHours;
                                string[] managerName = timesheetDb.ADUsers.Find(application.ApprovedBy)
                                                                  .UserName.Split(' ');
                                dr["Approved By"] = managerName[1] + ", " + managerName[0];

                                dt.Rows.Add(dr);

                                // initialise variables
                                totalHours = records[i].LeaveTime;
                                previousType = records[i].LeaveType.Value;
                                startDate = String.Format("{0:dd/MM/yy}", records[i].RecordDate);
                                endDate = String.Format("{0:dd/MM/yy}", records[i].RecordDate);
                            }
                        }
                    }
                }
            }
            dt.DefaultView.Sort = "Surname";
            dt = dt.DefaultView.ToTable(true);

            return dt;
        }

        private FileContentResult GetCSV(DataTable dt, string year, string period)
        {
            StringBuilder sb = new StringBuilder();
            IEnumerable<string> columnNames = dt.Columns.Cast<DataColumn>()
                                                .Select(column => column.ColumnName);
            sb.AppendLine(string.Join(",", columnNames));

            foreach (DataRow row in dt.Rows)
            {
                IEnumerable<string> fields = row.ItemArray.Select(field => field.ToString());
                sb.AppendLine(string.Join(",", fields));
            }

            // Filename
            string filename = "Payroll_" + period.PadLeft(2, '0') + year.Substring(year.Length - 2) + ".csv";

            return File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", filename);
        }

        // Sum
        //public async Task<FileContentResult> PayrollExportResult(int year, int period)
        //{
        //    //update the ADUser from AD first before exporting the csv file
        //    await ADUser.GetADUser();


        //    DateTime StartDate = PayPeriod.GetStartDay(year, period);
        //    DateTime EndDate = PayPeriod.GetEndDay(year, period);

        //    List<Payroll> payrolls = (from t1 in timesheetDb.TimeRecords
        //                              where t1.LeaveType != null
        //                                    && t1.RecordDate >= StartDate
        //                                    && t1.RecordDate <= EndDate
        //                              group t1 by new { t1.UserID, t1.LeaveType } into t2
        //                              join u in timesheetDb.ADUsers on t2.FirstOrDefault().UserID equals u.Email // Email used as UserID in TimeRecord table
        //                              select new Payroll
        //                              {
        //                                  UserName = u.UserName,
        //                                  JobCode = u.JobCode,
        //                                  EmployeeID = u.EmployeeID,
        //                                  RecordDate = t2.FirstOrDefault().RecordDate,
        //                                  Flexi = t2.FirstOrDefault().Flexi,
        //                                  LeaveTime = t2.Sum(t => t.LeaveTime),
        //                                  LeaveType = t2.FirstOrDefault().LeaveType
        //                              }).ToList();

        //    DataTable dt = new DataTable();
        //    DataColumn LastName = new DataColumn("Employee Last Name", typeof(string));
        //    dt.Columns.Add(LastName);

        //    DataColumn FirstName = new DataColumn("Employee First Name", typeof(string));
        //    dt.Columns.Add(FirstName);

        //    DataColumn Job = new DataColumn("Job", typeof(string));
        //    dt.Columns.Add(Job);

        //    DataColumn Payroll = new DataColumn("Leave Type", typeof(string));
        //    dt.Columns.Add(Payroll);

        //    DataColumn Units = new DataColumn("Units", typeof(double));
        //    dt.Columns.Add(Units);

        //    DataColumn Notes = new DataColumn("Notes", typeof(string));
        //    dt.Columns.Add(Notes);

        //    DataColumn ID = new DataColumn("Employee Card ID", typeof(int));
        //    dt.Columns.Add(ID);

        //    if (payrolls != null)
        //    {
        //        for (int i = 0; i < payrolls.Count; i++)
        //        {
        //            DataRow dr = dt.NewRow();

        //            if(payrolls[i].UserName.Contains(' '))
        //            {
        //                string[] words = payrolls[i].UserName.Split(' ');
        //                dr["Employee Last Name"] = words[1];
        //                dr["Employee First Name"] = words[0];
        //            }
        //            else
        //                dr["Employee Last Name"] = payrolls[i].UserName;

        //            dr["Job"] = payrolls[i].JobCode;
        //            dr["Employee Card ID"] = payrolls[i].EmployeeID;
        //            dr["Notes"] = null;

        //            dr["Leave Type"] = payrolls[i].LeaveType.GetDisplayName();
        //            dr["Units"] = payrolls[i].LeaveTime;
        //            dt.Rows.Add(dr);
        //        }
        //    }

        //    return GetCSV(dt);
        //}
    }
}
