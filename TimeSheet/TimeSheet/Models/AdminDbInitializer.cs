using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using TimeSheet.Models;

namespace TimeSheet.Models
{
    public class AdminDbInitializer : DropCreateDatabaseAlways<AdminDb>
    {
        protected override void Seed(AdminDb context)
        {
            base.Seed(context);
            //Initializer holiday table
            List<Holiday> holidayList = PayPeriod.GetHoliday();
            foreach (Holiday item in holidayList)
            {
                context.Holidays.Add(item);
            }

            //Initalizer Email setting table
            EmailSetting model = new EmailSetting();
            context.EmailSetting.Add(model);

            context.SaveChanges();
        }
    }
}