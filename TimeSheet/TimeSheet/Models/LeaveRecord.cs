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
        public Double AvailableLeaveMinuites { get; set; }
         
        [NotMapped]
        public TimeSpan AvailableLeaveTime
        {
            get { return TimeSpan.FromMinutes(AvailableLeaveMinuites); }
            set { AvailableLeaveMinuites = value.TotalMinutes; }
        }

        [NotMapped]
        public int ExtractHours
        {
            get { return (int)AvailableLeaveTime.TotalHours; }
            set { AvailableLeaveMinuites = value * 60 + AvailableLeaveMinuites % 60; }
        }

        [NotMapped]
        public int ExtractMins
        {
            get { return AvailableLeaveTime.Minutes; }
            set { AvailableLeaveMinuites = AvailableLeaveMinuites - (AvailableLeaveMinuites % 60) + value; }
        }
        
        public void SetMinutes(int hours, int mins)
        {
            AvailableLeaveMinuites = 60 * hours + mins;
        }
    }
}