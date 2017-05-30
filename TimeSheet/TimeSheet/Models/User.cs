using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class User
    {
        public string UserName { get; set; }

        public string JobCode { get; set; }

        public string Email { get; set; }

        public int EmployeeID { get; set; }

        public virtual ICollection<TimeRecord> TimeRecords { get; set; }
    }
}