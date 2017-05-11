using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class Email
    {
        [Required, EmailAddress]
        public string FromEmail { get; set; }

        public string subject { get; set; }

        [Required]
        public string Message { get; set; }
    }
}