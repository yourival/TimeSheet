using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TimeSheet.Models
{
    public class User
    {
        [Key]
        public int ID { get; set; }

        public string UserName { get; set; }

        public string JobCode { get; set; }

        public string Email { get; set; }

        public int EmployeeID { get; set; }

        public virtual ICollection<TimeRecord> TimeRecords { get; set; }
    }
}