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
            context.UserRoleSettings.Add(new UserRoleSetting()
            {
                UserID = "d.yang@m.nantien.edu.au",
                UserName = "Dawen Yang",
                IsAdmin = true,
                IsAccountant = false,
                IsManager = true
            });
            context.UserRoleSettings.Add(new UserRoleSetting()
            {
                UserID = "r.lin@m.nantien.edu.au",
                UserName = "Robin Lin",
                IsAdmin = true,
                IsAccountant = false,
                IsManager = true
            });
            context.SaveChanges();
        }
    }
}
    