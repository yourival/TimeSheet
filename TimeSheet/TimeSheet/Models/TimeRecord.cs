using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class TimeRecord
    {
        public TimeRecord (DateTime date)
        {
            StartTime = new DateTime(date.Year, date.Month, date.Day, 9, 0, 0);
            LunchBreak = new TimeSpan(0, 30, 0);
            EndTime = new DateTime(date.Year, date.Month, date.Day, 17, 0, 0);
            IsHoliday = false;
        }

        public int id { get; set; }
        public string UserID { get; set; }

        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        [DataType(DataType.Time)]
        public DateTime StartTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        [DataType(DataType.Time)]
        public DateTime EndTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        ////[DataType(DataType.Time)]
        public TimeSpan LunchBreak { get; set; }

        public bool Flexi { get; set; }
        public _leaveType LeaveType { get; set; }

        public bool IsHoliday { get; set; }


        //[DisplayFormat(DataFormatString = "{0:HH:mm}", ApplyFormatInEditMode = true)]
        //[DataType(DataType.Time)]
        public TimeSpan LeaveTime { get; set; }

        public TimeSpan GetWorkHours ()
        {
            return StartTime-EndTime-LunchBreak;
        }
    }
}