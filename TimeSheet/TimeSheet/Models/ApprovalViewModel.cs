using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class ApprovalViewModel
    {
        public LeaveApplicationViewModel UserApplicationVM { get; set; }
        public List<TimeRecord> TakenLeaves { get; set; }

        public ApprovalViewModel(): base() {
            UserApplicationVM = new LeaveApplicationViewModel();
            TakenLeaves = new List<TimeRecord>();
        }
    }
}