using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace TimeSheet.Models
{
    public class AdminDbInitializer : CreateDatabaseIfNotExists<AdminDb>
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
                ManagerName = "Dawen Yang",
                IsAdmin = true
            });
            context.ManagerSetting.Add(new Manager()
            {
                ManagerID = "r.lin@m.nantien.edu.au",
                ManagerName = "Robin Lin",
                IsAdmin = true
            });
            context.SaveChanges();
        }
    }
}
    