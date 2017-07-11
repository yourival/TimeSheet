using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TimeSheet.Models
{
    public class TimeSheetContainer
    {
        public TimeRecordForm TimeRecordForm { get; set; }
        public List<TimeRecord> TimeRecords { get; set; }

        public IEnumerable<SelectListItem> YearList { get; set; }

        public IEnumerable<SelectListItem> PeriodList { get; set; }

    }
}