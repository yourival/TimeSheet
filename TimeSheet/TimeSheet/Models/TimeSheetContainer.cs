using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class TimeSheetContainer
    {
        public TimeRecordForm TimeRecordForm { get; set; }
        public List<TimeRecord> TimeRecords { get; set; }
    }
}