using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class LeaveApplication
    {
        public enum _leaveType { sick, flexi, annual }
        public enum _status { approved, rejected, submited }
        
        public LeaveApplication(DateTime date)
        {
            StartTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0);
            EndTime = new DateTime(date.Year, date.Month, date.Day, 17, 0, 0);
        }

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