using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using TimeSheet.Models;

namespace TimeSheet
{
    public class AADHelper
    {
        public static String GetUserRole(String email)
        {
            AdminDb context = new AdminDb();
            var manager = (from m in context.ManagerSetting
                           where m.ManagerID == email
                           select m).FirstOrDefault();
            if (manager != null)
            {
                if (manager.IsAdmin)
                    return "IsAdmin";
                else
                    return "IsManager";
            }
            else
            {
                return "Normal User";
            }
        }
    }
}