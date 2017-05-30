
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class EmailSetting
    {
        public EmailSetting()
        {
            FromEmail = "d.yang@m.nantien.edu.au";
            Password = "Y137196506dw";
            Subject = "Leave Application";
            Message = "Please Click the link below to approve the leave application";
            SMTPHost = "smtp.office365.com";
            SMTPPort = 587;
            EnableSsl = true;
        }

        [Key]
        public int id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "From Email")]
        public string FromEmail { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public string Subject { get; set; }

        public string Message { get; set; }

        [Required]
        [Display(Name = "SMTP Host")]
        public string SMTPHost { get; set; }

        [Required]
        [Display(Name = "SMTP Port")]
        public int SMTPPort { get; set; }

        [Required]
        [Display(Name = "Enable SSL")]
        public bool EnableSsl { get; set; }
    }
}