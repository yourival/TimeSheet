using System;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace TimeSheet.Models
{
    public class LeaveRecord
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Key, Column(Order = 0)]
        public string UserID { get; set; }
        [Key, Column(Order = 1)]
        public _leaveType LeaveType { get; set; }

        [RegularExpression(@"^(\d*(\.[05])?)$", ErrorMessage = "Fill in a number that is a multiple of 0.5")]
        public Double AvailableLeaveHours { get; set; }

        [NotMapped]
        public TimeSpan AvailableLeaveTime
        {
            get { return TimeSpan.FromHours(AvailableLeaveHours); }
            set { AvailableLeaveHours = value.TotalHours; }
        }
    }
}