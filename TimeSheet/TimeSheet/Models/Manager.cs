using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class Manager
    {
        [Key]
        public int id { get; set; }

        [Required]
        [EmailAddress]
        public String ManagerID { get; set; }

        [Required]
        public String ManagerName { get; set; }
    }
}