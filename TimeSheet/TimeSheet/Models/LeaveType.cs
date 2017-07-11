using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class LeaveType
    {
        [Key]
        public int value { get; set; }
        public string name { get; set; }
    }
}