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
        public string UserId { get; set; }
        public string UserName { get; set; }
        public double[] Balances { get; set; }
        public string Note { get; set; }

        public LeaveBalanceViewModel()
        {
            UserId = string.Empty;
            UserName = string.Empty;
            Note = string.Empty;
            Balances = new double[3];
        }
    }
}