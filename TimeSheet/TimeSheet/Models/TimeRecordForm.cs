using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace TimeSheet.Models
{
    public class TimeRecordForm
    {
        [Key]
        public int TimeRecordFormId { get; set; }

        public int Year { get; set; }
        public int Period { get; set; }
        public string UserID { get; set; }

        public string ManagerID { get; set; }

        public double TotalWorkingHours { get; set; }

        public double TotalLeaveHours { get; set; }

        // if status is null, it is saved for further editing
        public _status? status { get; set; }

        public DateTime SubmittedTime { get; set; }

        public DateTime? ApprovedTime { get; set; }

        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }
    }
}