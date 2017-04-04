using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public enum _leaveType { sick, flexi, annual }
    public enum _status { approved, rejected, submited }

    public class LeaveApplication
    {
        [Key]
        public int id { get; set; }
        public string UserID { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartTime { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndTime { get; set; }

        public _leaveType leaveType { get; set; }
        public string ManagerID { get; set; }
        public _status status { get; set; }

    }
}