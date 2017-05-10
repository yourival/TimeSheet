using System;
using System.Web;
using System.Collections.Generic;

namespace TimeSheet.Models
{
    public class LeaveApplicationViewModel
    {
        public LeaveApplication LeaveApplication { get; set; }
        public List<TimeRecord> TimeRecords { get; set; }
        public List<LeaveRecord> LeaveRecords { get; set; }

        public LeaveApplicationViewModel(): base() {}
    }
}
