using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using System.Web;

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

        [DataType(DataType.Date)]
        [Display(Name = "Start Time")]
        public DateTime StartTime { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "End Time")]
        public DateTime EndTime { get; set; }

        [Display(Name = "Total Hours")]
        public double TotalLeaveTime { get; set; }

        [Display(Name = "Type")]
        public _leaveType leaveType { get; set; }

        public string ManagerID { get; set; }

        [Display(Name = "Status")]
        public _status status { get; set; }

        public string Comment { get; set; }

        public List<TimeRecord> GetTimeRecords()
        {
            List<TimeRecord> records = new List<TimeRecord>();
            DateTime start = StartTime;
            DateTime end = EndTime;
            TimeSheetDb contextDb = new TimeSheetDb();

            for (int i = 0; i <= (end - start).Days; i++)
            {
                DateTime currentDate = start.AddDays(i);
                var newTimeRecord = (from r in contextDb.TimeRecords
                                     where DbFunctions.TruncateTime(r.RecordDate) == currentDate.Date &&
                                           r.UserID == UserID
                                     select r).FirstOrDefault();
                if (newTimeRecord != null)
                {
                    if (!newTimeRecord.IsHoliday)
                        records.Add(newTimeRecord);
                }
            }

            return records;
        }
    }
}