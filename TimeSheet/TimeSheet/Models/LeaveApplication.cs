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
    public enum _leaveType { none, sick, flexi, annual }
    public enum _status { approved, rejected, submited, modified }

    public class LeaveApplication
    {
        [Key]
        [Display(Name = "#")]
        public int id { get; set; }
        [Display(Name = "User ID")]
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
        public double TotalTime { get; set; }

        [Required]
        [Display(Name = "Type")]
        public _leaveType leaveType { get; set; }

        public string ManagerID { get; set; }

        [Display(Name = "Status")]
        public _status status { get; set; }

        [DataType(DataType.MultilineText)]
        public string Comment { get; set; }

        public virtual ICollection<LeaveAttachment> Attachments { get; set; }

        public List<TimeRecord> GetTimeRecords()
        {
            List<TimeRecord> records = new List<TimeRecord>();
            DateTime start = StartTime;
            DateTime end = EndTime;
            TimeSheetDb contextDb = new TimeSheetDb();
            Debug.WriteLine(contextDb.TimeRecords.FirstOrDefault());
            for (int i = 0; i <= (end - start).Days; i++)
            {
                DateTime currentDate = start.AddDays(i);
                var newTimeRecord = (from r in contextDb.TimeRecords
                                     where DbFunctions.TruncateTime(r.RecordDate) == currentDate.Date /*&&
                                           r.UserID == UserID*/
                                     select r).FirstOrDefault();
                if (newTimeRecord != null)
                {
                    if (!newTimeRecord.IsHoliday)
                        records.Add(newTimeRecord);
                }
            }

            return records;
        }

        public static IEnumerable<SelectListItem> GetLeaveTypeItems()
        {
            IEnumerable<SelectListItem> listItems =
                Enum.GetValues(typeof(_leaveType))
                .Cast<_leaveType>()
                .Where(e => e != _leaveType.none)
                .Select(e => new SelectListItem
                {
                    Text = e.ToString(),
                    Value = ((int)e).ToString()
                });

            return listItems;
        }
    }
}