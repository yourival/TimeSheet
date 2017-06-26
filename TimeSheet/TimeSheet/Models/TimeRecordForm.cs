using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace TimeSheet.Models
{
    public class TimeRecordForm
    {

        public enum _formstatus { approved, rejected, modified, submitted }

        public enum _sumbitstatus { submitted, saved}

        [Key]
        public int TimeRecordFormID { get; set; }

        public int Year { get; set; }
        public int Period { get; set; }
        public string UserID { get; set; }

        public string ManagerID { get; set; }

        public double TotalWorkingHours { get; set; }

        public double TotalLeaveHours { get; set; }

        public _formstatus FormStatus { get; set; }

        public _sumbitstatus SumbitStatus { get; set; }

        public DateTime SubmitTime { get; set; }

        [DataType(DataType.MultilineText)]
        public string Comments { get; set; }
        
    }
}