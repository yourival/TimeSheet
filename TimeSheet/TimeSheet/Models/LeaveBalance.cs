using System;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TimeSheet.Models
{
    public class LeaveBalance
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key, Column(Order = 0)]
        public string UserID { get; set; }
        [Key, Column(Order = 1)]
        public _leaveType LeaveType { get; set; }

        public string UserName { get; set; }
        
        public Double AvailableLeaveHours { get; set; }
    }
}