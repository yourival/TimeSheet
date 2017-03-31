using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace TimeSheet.Models
{
    public class TimeSheetContext : DbContext
    {
        public DbSet<TimeRecord> TimeRecords { get; set; }
    }
}