using System;
using System.Web;
using System.Collections.Generic;

namespace TimeSheet.Models
{
    public class LeaveApplicationViewModel
    {
        public LeaveApplication LeaveApplication { set; get; }
        public virtual List<TimeRecord> Records { set; get; }

        public LeaveApplicationViewModel(): base() {}
    }
}
