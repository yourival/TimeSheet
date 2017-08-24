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

        [Display(Name = "Email")]
        public string UserID { get; set; }

        [Display(Name = "Employee")]
        public string UserName { get; set; }

        [NotMapped]
        public List<string> _managerIDs { get; set; }
        public string ManagerIDs
        {
            get { return string.Join("/", _managerIDs); }
            set { _managerIDs = value.Split('/').ToList(); }
        }

        [Display(Name = "Total Working Hours")]
        public double TotalWorkingHours { get; set; }

        [Display(Name = "Total Leave Hours")]
        public double TotalLeaveHours { get; set; }

        [Display(Name = "Status")]
        public _status status { get; set; }

        [Display(Name = "Submitted Time")]
        public DateTime SubmittedTime { get; set; }

        [Display(Name = "Approved Time")]
        public DateTime? ApprovedTime { get; set; }

        [Display(Name = "Approved By")]
        public string ApprovedBy { get; set; }

        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }
    }
}