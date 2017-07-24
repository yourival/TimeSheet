using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class ApprovalViewModel
    {
        public LeaveApplication LeaveApplication { get; set; }
        public List<TimeRecord> TimeRecords { get; set; }
        public List<Tuple<DateTime, string, double>> TakenLeaves { get; set; }

        public ApprovalViewModel(): base() {
            LeaveApplication = new LeaveApplication();
            TimeRecords = new List<TimeRecord>();
            TakenLeaves = new List<Tuple<DateTime, string, double>>();
        }
    }
}