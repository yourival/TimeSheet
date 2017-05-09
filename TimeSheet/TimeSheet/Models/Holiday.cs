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
        public DateTime HolidayDate { get; set; }

        public string HolidayName { get; set; }
    }
}