using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace TimeSheet.Models
{
    public class AdminDbInitializer : DropCreateDatabaseAlways<AdminDb>
    {
        // Puts sample data into the database
        protected override void Seed(AdminDb context)
        {
            // Initialise holidays
            List<Holiday> holidayList = PayPeriod.GetHoliday();
            holidayList.ForEach(h => context.Holidays.Add(h));

            // Initialise manager
            context.ManagerSetting.Add(new Manager()
            {
                ManagerID = "d.yang@m.nantien.edu.au",
                ManagerName = "Dawen Yang"
            });

            context.EmailSetting.Add(new EmailSetting()
            {
                FromEmail = "d.yang@m.nantien.edu.au",
                Password = "Y137196506dw",
                Message = "Please Click the link below to approve the leave application",
                SMTPHost = "smtp.office365.com",
                SMTPPort = 587
            });
            context.SaveChanges();
        }
    }
}
    