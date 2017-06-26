using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;

namespace TimeSheet
{
    public class AADHelper
    {
        public static bool IsUserAdmin(String email)
        {
            return true;
        }

        public static bool IsUserManager(String email)
        {
            return true;
        }
    }
}