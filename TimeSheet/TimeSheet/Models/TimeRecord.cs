using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Configuration;

namespace TimeSheet.Models
{
    public class TimeRecord
    {
        public TimeRecord() { }
        public TimeRecord (DateTime date)
        {
            RecordDate = date.Date;
            StartTime = TimeSpan.FromHours(9);
            LunchBreak = TimeSpan.FromMinutes(30);
            EndTime = TimeSpan.FromHours(17);
            IsHoliday = false;
        }

        public int id { get; set; }
        public string UserID { get; set; }
        public bool IsHoliday { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}",ApplyFormatInEditMode = true)]
        [DataType(DataType.Date)]
        public DateTime RecordDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan StartTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan EndTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true)]
        public TimeSpan LunchBreak { get; set; }

        public bool Flexi { get; set; }
        public _leaveType LeaveType { get; set; }

        [RegularExpression(@"^([0-7](\.[05])?)$", ErrorMessage = "fill in a number that is a multiple of 0.5 and not larger than 7.5")]
        public double LeaveTime { get; set; }

        public TimeSpan GetWorkHours ()
        {
            return StartTime-EndTime-LunchBreak;
        }
    }
}