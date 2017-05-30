﻿using System;
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
            // Initialise users
            context.Users.Add(new User()
            {
                UserId = "r.lin@m.nantien.edu.au",
                FirstName = "Yichia",
                LastName = "Lin",
                JobCode = "102"
            });
            context.Users.Add(new User()
            {
                UserId = "y.ben@m.nantien.edu.au",
                FirstName = "Yanhong",
                LastName = "Ben",
                JobCode = "110A"
            });
            context.SaveChanges();

            // Initialise holidays
            List<Holiday> holidayList = PayPeriod.GetHoliday();
            holidayList.ForEach(h => context.Holidays.Add(h));
            context.SaveChanges();

            // Initialise manger
            context.ManagerSetting.Add(new Manager()
            {
                ManagerID = "d.wang@m.nantien.edu.au",
                ManagerName = "Dawen"
            });
        }
    }
}
    