using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TimeSheet.Models
{
    public class User
    {
        [Key, Column(Order = 0)]
        public string Email { get; set; }

        public string UserName { get; set; }

        public string JobCode { get; set; }

        public int EmployeeID { get; set; }

        public virtual ICollection<TimeRecord> TimeRecords { get; set; }
    }
}