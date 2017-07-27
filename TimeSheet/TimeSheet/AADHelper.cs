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
        public static UserRoleSetting GetUserRole(String email)
        {
            UserRoleSetting userRole = null;
            using (AdminDb context = new AdminDb())
            {
                userRole = (from m in context.UserRoleSettings
                            where m.UserID == email
                            select m).FirstOrDefault();
            }
            return userRole;
        }
    }
}