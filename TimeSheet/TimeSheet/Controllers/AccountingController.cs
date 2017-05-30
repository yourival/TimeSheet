using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Web;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class AccountingController : Controller
    {
        private enum _payrollType { basic, holiday, publicHoliday, sick, flexi };
        // GET: Accounting
        public ActionResult Index()
        {
            return View();
        }
        public FileContentResult DownloadCSV()
        {
            StringBuilder stringBuilder = new StringBuilder();
            WriteColumnName(stringBuilder);
            WriteRecords(stringBuilder);

            return File(new UTF8Encoding().GetBytes(stringBuilder.ToString()), "text/csv", "Report123.csv");
        }

        private void WriteRecords(StringBuilder stringBuilder)
        {
            

        }
        
        private void WriteColumnName(StringBuilder stringBuilder)
        {
            string columnNames = "Employee Co./Last Name, Employee First Name, Payroll Category, Job, Notes, Date, Units, Employee Card ID";
            stringBuilder.AppendLine(columnNames);
        }
    }
}