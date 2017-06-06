using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class Holiday
    {
        public int id { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Holiday Date")]
        public DateTime HolidayDate { get; set; }
        [Display(Name = "Holiday Name")]
        public string HolidayName { get; set; }
    }
}