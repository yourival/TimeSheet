﻿using System;
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
            EndTime = TimeSpan.FromHours(17);
            LunchBreak = 0.5;
            IsHoliday = false;
            Flexi = false;
            LeaveTime = 0;
            LeaveType = _leaveType.none;
        }

        public int id { get; set; }
        public string UserID { get; set; }
        public bool IsHoliday { get; set; }

        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [DataType(DataType.Date)]
        public DateTime RecordDate { get; set; }

        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true, NullDisplayText = "None")]
        public TimeSpan? StartTime { get; set; }

        [DisplayFormat(DataFormatString = "{0:hh\\:mm}", ApplyFormatInEditMode = true, NullDisplayText = "None")]
        public TimeSpan? EndTime { get; set; }

        [RegularExpression(@"^([0-7](\.[05])?)$", ErrorMessage = "Fill in a number that is a multiple of 0.5 and not larger than 7.5")]
        public double LunchBreak { get; set; }

        public bool Flexi { get; set; }
        public _leaveType LeaveType { get; set; }

        [RegularExpression(@"^([0-7](\.[05])?)$", ErrorMessage = "Fill in a number that is a multiple of 0.5 and not larger than 7.5")]
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

        // Convinent function to set properties related to attendence
        public void SetAttendence(double? startHour, double? endHour, double lunchHour)
        {
            if (startHour != null)
                StartTime = TimeSpan.FromHours(startHour.Value);
            else
                StartTime = null;

            if (endHour != null)
                EndTime = TimeSpan.FromHours(endHour.Value);
            else
                EndTime = null;

            LunchBreak = lunchHour;
        }

        public virtual ICollection<User> Users { get; set; }
    }
}