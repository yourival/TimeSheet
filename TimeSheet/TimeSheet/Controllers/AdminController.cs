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
    /// <summary>
    ///     Controller to handle tasks other than leave application and approval
    /// </summary>
    [Authorize]
    public class AdminController : Controller
    {
        private TimeSheetDb timesheetDb = new TimeSheetDb();
        private AdminDb adminDb = new AdminDb();

        /// <summary>
        ///     Creates an index view to navigate features for site settings.
        /// </summary>
        /// <returns>A view with grid buttons of navigation.</returns>
        // GET: Admin
        [AuthorizeUser(Roles = "Manager")]
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        ///     Creates a view to update holiday automatically.
        /// </summary>
        /// <returns>A view with detail of holidays stored in DB.</returns>
        // GET: Admin/Holidays
        [AuthorizeUser(Roles = "Admin")]
        public ActionResult Holidays()
        {
            List<Holiday> holidayList = adminDb.Holidays.ToList();
            return View(holidayList);
        }

        /// <summary>
        ///     Download and update <see cref="Holiday"/> from government website.
        /// </summary>
        /// <returns>Refreshed holidays view.</returns>
        [ValidateAntiForgeryToken]
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

        /// <summary>
        ///     Creates a view for an administrator to edit email settings for sending emails of
        ///     HR applications and approvals.
        /// </summary>
        /// <returns>A view with details to edit <see cref="EmailSetting" />.</returns>
        // GET: Admin/EmailSetting
        [AuthorizeUser(Roles = "Admin")]
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

        /// <summary>
        ///     Processes editing of <see cref="EmailSetting"/> for sending emails of
        ///     HR applications and approvals.
        /// </summary>
        /// <param name="model">Unique identifier of the <see cref="EmailSetting" />.</param>
        /// <returns>A view with refreshed list of <see cref="EmailSetting" />.</returns>
        // POST: Admin/EmailSetting
        [ValidateAntiForgeryToken]
        [AuthorizeUser(Roles = "Admin")]
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

        /// <summary>
        ///     Creates a view to edit/delete all existing <see cref="UserRoleSetting" />.
        /// </summary>
        /// <returns>A view with list of all <see cref="UserRoleSetting" /> objects.</returns>
        // GET: Admin/UserRoleSetting
        [AuthorizeUser(Roles = "Admin")]
        public ActionResult UserRoleSetting()
        {
            List<UserRoleSetting> UserRoleList = adminDb.UserRoleSettings.ToList();
            return View(UserRoleList);
        }

        /// <summary>
        ///     Creates a view to create a <see cref="UserRoleSetting" /> (under construction).
        /// </summary>
        /// <returns>A view with details of <see cref="UserRoleSetting" /> to create.</returns>
        // GET: Admin/CreateUserRole
        [AuthorizeUser(Roles = "Admin")]
        public ActionResult CreateUserRole()
        {
            return View();
        }

        /// <summary>
        ///     Processes creating of <see cref="UserRoleSetting"/>.
        /// </summary>
        /// <param name="model">The <see cref="UserRoleSetting"/> to be created.</param>
        /// <returns>A view with list of all <see cref="UserRoleSetting" /> objects.</returns>
        // POST: Admin/CreateUserRole
        [AuthorizeUser(Roles = "Admin")]
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

        /// <summary>
        ///     Creates a view to edit an existing <see cref="UserRoleSetting" />.
        /// </summary>
        /// <param name="id">The identifier (email) of the <see cref="UserRoleSetting" />.</param>
        /// <returns>A view with details of an existing <see cref="UserRoleSetting" /> to edit.</returns>
        // GET: Admin/EditUserRole
        [AuthorizeUser(Roles = "Admin")]
        public ActionResult EditUserRole(int id)
        {
            UserRoleSetting model = adminDb.UserRoleSettings.Find(id);
            if (model == null)
            {
                return HttpNotFound();
            }
            return View(model);
        }

        /// <summary>
        ///     Processes editing of <see cref="UserRoleSetting"/>.
        /// </summary>
        /// <param name="model">The <see cref="UserRoleSetting"/> to be edited.</param>
        /// <returns>A view with list of all <see cref="UserRoleSetting" /> objects.</returns>
        // POST: Admin/EditUserRole
        [AuthorizeUser(Roles = "Admin")]
        [HttpPost]
        public ActionResult EditUserRole(UserRoleSetting model)
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

        /// <summary>
        ///     Processes deleting of an exisitng <see cref="UserRoleSetting"/>.
        /// </summary>
        /// <param name="id">The identifier (email) of the <see cref="UserRoleSetting"/>.</param>
        /// <returns>A refreshed view with list of all <see cref="UserRoleSetting" /> objects.</returns>
        [AuthorizeUser(Roles = "Admin")]
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

        /// <summary>
        ///     Creates a view to display a payroll summary with details for a specific pay period.
        ///     The report can also be output with a professional format.
        /// </summary>
        /// <returns>A payroll summary with details of HR applications.</returns>
        // GET: Admin/PayrollExport
        [AuthorizeUser(Roles = "Manager, Accountant")]
        public ActionResult PayrollExport()
        {
            ViewBag.Year = PayPeriod.GetYearItems();
            return View();
        }

        /// <summary>
        ///     Dispay a list of pay periods when a year is selected.
        ///     The year is set to the current year by default.
        /// </summary>
        /// <param name="year">The selected year.</param>
        /// <returns>A dropdown list of pay periods in the selected year.</returns>
        public ActionResult SelectYear(int? year)
        {
            TimeSheetContainer model = new TimeSheetContainer();
            model.PeriodList = PayPeriod.GetPeriodItems((year == null) ? DateTime.Now.Year : year.Value);
            return PartialView("_SelectYear", model);
        }

        /// <summary>
        ///     Creates a payroll summary report with details for a specific pay period
        ///     in CSV format.
        /// </summary>
        /// <param name="year">The year of pay period.</param>
        /// <param name="period">The number of a pay period.</param>
        /// <returns>A payroll summary with details of HR applications in CSV format.</returns>
        public async Task<FileContentResult> CSVExport(string year, string period)
        {
            // update the ADUser from AD first before exporting the csv file
            await ADUser.GetADUser();

            DataTable dt = GetDataTable(year, period);
            return GetCSV(dt, year, period);
        }

        /// <summary>
        ///     Creates a preview of a payroll summary report with details for
        ///     a specific pay period before output.
        /// </summary>
        /// <param name="year">The year of pay period.</param>
        /// <param name="period">The number of a pay period.</param>
        /// <returns>A partial view of payroll summary with details of HR applications.</returns>
        // GET: Admin/_Preview
        public ActionResult PayrollPreview(string year, string period)
        {
            DataTable dt = GetDataTable(year, period);
            return PartialView("_Preview", dt);
        }


        /// <summary>
        ///     Gets the <see cref="DataTable"/> of payroll summary in a specific period.
        /// </summary>
        /// <param name="year">The year of pay period.</param>
        /// <param name="period">The number of a pay period.</param>
        /// <returns>A <see cref="DataTable"/> of payroll summary in a specific period.</returns>
        private DataTable GetDataTable(string year, string period)
        {
            int y = Convert.ToInt32(year);
            int p = Convert.ToInt32(period);
            DateTime startPeriod = PayPeriod.GetStartDay(y, p);
            DateTime endPeriod = PayPeriod.GetEndDay(y, p);
            ViewBag.Period = String.Format("{0:dd/MM/yy}", startPeriod) + " - " +
                             String.Format("{0:dd/MM/yy}", endPeriod);

            // Select applications that's in this period, or has been approved in this period
            List<LeaveApplication> applications = (from a in timesheetDb.LeaveApplications
                                                  where a.status != _status.rejected &&
                                                        (!(a.EndTime < startPeriod ||
                                                           a.StartTime > endPeriod) ||
                                                          (a.ApprovedTime >= startPeriod &&
                                                           a.ApprovedTime <= endPeriod))
                                                   select a).ToList();

            DataTable dt = new DataTable();
            // Initialise columns
            dt.Columns.Add(new DataColumn("Application ID", typeof(string)));
            dt.Columns.Add(new DataColumn("Employee Card ID", typeof(string)));
            dt.Columns.Add(new DataColumn("Surname", typeof(string)));
            dt.Columns.Add(new DataColumn("First Name", typeof(string)));
            dt.Columns.Add(new DataColumn("Position", typeof(string)));
            dt.Columns.Add(new DataColumn("Date or Period", typeof(string)));
            dt.Columns.Add(new DataColumn("Total Hours", typeof(double)));
            dt.Columns.Add(new DataColumn("Leave Type / Additional Hours", typeof(string)));
            dt.Columns.Add(new DataColumn("Approved By", typeof(string)));
            dt.Columns.Add(new DataColumn("Note", typeof(string)));

            // Insert rows
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

                    bool multiplyTypes = records.Any(r => r.LeaveType != application.leaveType);
                    // If the application is within the period and contains only 1 type
                    if ((PayPeriod.GetPeriodNum(application.StartTime) == PayPeriod.GetPeriodNum(application.EndTime)) &&
                        !multiplyTypes)
                    {
                        DataRow dr = dt.NewRow();
                        dr["Employee Card ID"] = user.EmployeeID;
                        dr["Surname"] = words[1];
                        dr["First Name"] = words[0];
                        dr["Application ID"] = application.id;
                        dr["Position"] = user.JobCode;
                        dr["Date or Period"] = String.Format("{0:dd/MM/yy}", application.StartTime);
                        if(application.StartTime != application.EndTime)
                            dr["Date or Period"] += " - " + String.Format("{0:dd/MM/yy}", application.EndTime);
                        dr["Total Hours"] = application.TotalLeaveTime;
                        dr["Leave Type / Additional Hours"] = application.leaveType.GetDisplayName();

                        if (application.status == _status.approved)
                        {
                            string[] managerName = timesheetDb.ADUsers.Find(application.ApprovedBy).UserName.Split(' ');
                            dr["Approved By"] = managerName[1] + ", " + managerName[0];
                            if (PayPeriod.GetPeriodNum(application.EndTime) < PayPeriod.GetPeriodNum(application.ApprovedTime.Value))
                            {
                                if(PayPeriod.GetPeriodNum(application.ApprovedTime.Value) == p)
                                    dr["Note"] = "Approved in this pay period";
                                else
                                {
                                    dr["Note"] = "Not approved yet";
                                    dr["Approved By"] = "";
                                }
                            }
                        }
                        else
                            dr["Note"] = "Not approved yet";

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
                                dr["Application ID"] = application.id;
                                dr["Position"] = user.JobCode;
                                dr["Leave Type / Additional Hours"] = previousType.GetDisplayName();
                                dr["Date or Period"] = startDate;
                                if (startDate != endDate)
                                    dr["Date or Period"] += " - " + endDate;
                                dr["Total Hours"] = totalHours;
                                if(application.ApprovedBy != null)
                                {
                                    string[] managerName = timesheetDb.ADUsers.Find(application.ApprovedBy)
                                                                      .UserName.Split(' ');
                                    dr["Approved By"] = managerName[1] + ", " + managerName[0];
                                }
                                if (application.ApprovedTime == null)
                                    dr["Note"] = "Not approved yet";
                                else if (PayPeriod.GetPeriodNum(application.EndTime) < PayPeriod.GetPeriodNum(application.ApprovedTime.Value))
                                    dr["Note"] = "Approved in this pay period";

                                dt.Rows.Add(dr);

                                // Initialise variables for comparison
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

        /// <summary>
        ///     Converts a <see cref="DataTable"/> of payroll summary into a CSV file.
        /// </summary>
        /// <param name="dt">The <see cref="DataTable"/> of the file.</param>
        /// <param name="year">The year of pay period.</param>
        /// <param name="period">The number of a pay period.</param>
        /// <returns>A CSV file of payroll summary.</returns>
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

            // Sets filename
            string filename = "Payroll_" + period.PadLeft(2, '0') + year.Substring(year.Length - 2) + ".csv";

            return File(new UTF8Encoding().GetBytes(sb.ToString()), "text/csv", filename);
        }

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
