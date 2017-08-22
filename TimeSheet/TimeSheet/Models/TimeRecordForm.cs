using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace TimeSheet.Models
{
    public class TimeRecordForm
    {
        [Key]
        public int TimeRecordFormId { get; set; }

        public int Year { get; set; }
        public int Period { get; set; }
        public string UserID { get; set; }

        [NotMapped]
        public List<string> _managerIDs { get; set; }
        public string ManagerIDs
        {
            get { return string.Join("/", _managerIDs); }
            set { _managerIDs = value.Split('/').ToList(); }
        }

        public double TotalWorkingHours { get; set; }

        public double TotalLeaveHours { get; set; }

        public _status status { get; set; }

        public DateTime SubmittedTime { get; set; }

        public DateTime? ApprovedTime { get; set; }

        public string ApprovedBy { get; set; }

        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }
    }
}