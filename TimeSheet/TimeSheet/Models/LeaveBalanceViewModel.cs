using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;

namespace TimeSheet.Models
{
    public class LeaveBalanceViewModel
    { 
        public int RowId { get; set; }
        public List<LeaveBalance> Balances { get; set; }
    }
}