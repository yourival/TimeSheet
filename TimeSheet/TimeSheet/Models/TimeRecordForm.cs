using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace TimeSheet.Models
{
    public class TimeRecordForm
    {

        public enum _formstatus { approved, rejected, submited }

        [Key]
        public int TimeRecordFormID { get; set; }

        public int Year { get; set; }
        public int Period { get; set; }
        public string UserID { get; set; }

        public string ManagerID { get; set; }

        public _formstatus status { get; set; }
    }
}