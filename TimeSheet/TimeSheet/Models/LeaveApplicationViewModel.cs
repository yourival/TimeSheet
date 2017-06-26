using System;
using System.Collections.Generic;
using System.Web;

namespace TimeSheet.Models
{
    public class LeaveApplicationViewModel
    {
        public LeaveApplication LeaveApplication { get; set; }
        public List<TimeRecord> TimeRecords { get; set; }
        public List<LeaveBalance> LeaveBalances { get; set; }
        public IList<HttpPostedFileBase> Attachments { get; set; }

        public LeaveApplicationViewModel(): base() { }
    }
}
