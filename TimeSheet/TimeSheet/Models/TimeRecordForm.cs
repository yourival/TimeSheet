using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;


namespace TimeSheet.Models
{
    public class TimeRecordForm
    {
        public TimeRecordForm()
        {
            ManagerID = "00000";
            status = _status.submited;
        }

        public enum _status { approved, rejected, submited }

        [Key]
        public int Year { get; set; }
        public int Period { get; set; }
        public string UserID { get; set; }

        public string ManagerID { get; set; }

        public _status status { get; set; }
    }
}