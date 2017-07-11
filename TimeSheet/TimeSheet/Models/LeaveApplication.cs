using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;
using System.Web.Mvc;
using System.Diagnostics;

namespace TimeSheet.Models
{
    public enum _leaveType
    {
        [Display(Name = "Sick")]
        sick,
        [Display(Name = "Flexi")]
        flexi,
        [Display(Name = "Annual")]
        annual,
        [Display(Name = "Leave Without Pay")]
        leaveWithoutPay,
        [Display(Name = "Compassionate Pay")]
        compassionatePay,
        [Display(Name = "Long Service Leave")]
        longServiceLeave,
        [Display(Name = "Jury Duty")]
        juryDuty,
        [Display(Name = "Parental Leave")]
        parentalLeave,
        [Display(Name = "Additional Hours")]
        additionalHours,
        [Display(Name = "Flexi Hours")]
        flexiHours,
    }
    public enum _status
    {
        [Display(Name = "Approved")]
        approved,
        [Display(Name = "Rejected")]
        rejected,
        [Display(Name = "Submited")]
        submited,
        [Display(Name = "Modified")]
        modified
    }

    public class LeaveApplication
    {
        [Key]
        [Display(Name = "#")]
        public int id { get; set; }
        [Display(Name = "User Email")]
        public string UserID { get; set; }

        [Display(Name = "User Name")]
        public string UserName { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartTime { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Total Hours")]
        public double TotalLeaveTime { get; set; }

        [Required]
        [Display(Name = "Type")]
        public _leaveType leaveType { get; set; }

        public string ManagerID { get; set; }

        [Display(Name = "Status")]
        public _status status { get; set; }

        public DateTime? ApprovedTime { get; set; }

        [DataType(DataType.MultilineText)]
        public string Comment { get; set; }

        public string OriginalBalances { get; set; }

        public string CloseBalances { get; set; }

        public virtual ICollection<LeaveAttachment> Attachments { get; set; }

        public List<TimeRecord> GetTimeRecords()
        {
            TimeSheetDb contextDb = new TimeSheetDb();
            List<TimeRecord> records = (from r in contextDb.TimeRecords
                                    where r.RecordDate >= StartTime &&
                                          r.RecordDate <= EndTime &&
                                         !r.IsHoliday &&
                                          r.UserID == UserID
                                    select r).ToList();

            return records ?? new List<TimeRecord>();
        }

        //public static IEnumerable<SelectListItem> GetLeaveTypeItems()
        //{
        //    IEnumerable<SelectListItem> listItems =
        //        Enum.GetValues(typeof(_leaveType))
        //        .Cast<_leaveType>()
        //        .Select(e => new SelectListItem
        //        {
        //            Text = e.ToString(),
        //            Value = ((int)e).ToString()
        //        });

        //    return listItems;
        //}

    }
}