using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TimeSheet.Models
{
    public class LeaveAttachment
    {
        public int Id { get; set; }
        
        [StringLength(255)]
        public string FileName { get; set; }

        [StringLength(100)]
        public string ContentType { get; set; }

        public byte[] Content { get; set; }

        public int LeaveApplicationId { get; set; }
        public virtual LeaveApplication LeaveApplication { get; set; }
    }
}