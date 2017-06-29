using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class Payroll
    {
        public string UserName { get; set; }

        public string JobCode { get; set; }

        public int EmployeeID { get; set; }

        public bool IsHoliday { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [DataType(DataType.Date)]
        public DateTime RecordDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan? StartTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan? EndTime { get; set; }

        [RegularExpression(@"^([0-7](\.[05])?)$", ErrorMessage = "Fill in a multiple of 0.5 and not larger than 7.5")]
        public double LunchBreak { get; set; }

        public bool Flexi { get; set; }
        public _leaveType? LeaveType { get; set; }

        [RegularExpression(@"^([0-7](\.[05])?)$", ErrorMessage = "Fill in a multiple of 0.5 and not larger than 7.5")]
        public double LeaveTime { get; set; }

        // Automatically get work hours by attendence, or ignore attendence for casual workers
        private double CasualWorkHours;
        public double WorkHours
        {
            get
            {
                if (EndTime != null && StartTime != null)
                    return (EndTime.Value - StartTime.Value).TotalHours - LunchBreak;
                else
                    return CasualWorkHours;
            }
            set
            {
                if (StartTime != null)
                    EndTime = StartTime.Value.Add(TimeSpan.FromHours(value + LunchBreak));
                else if (EndTime != null)
                    StartTime = StartTime.Value.Subtract(TimeSpan.FromHours(value + LunchBreak));
                else
                    CasualWorkHours = value;
            }
        }
    }
}