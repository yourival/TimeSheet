﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace TimeSheet.Models
{
    public class TimeSheetDb : DbContext
    {
        public TimeSheetDb() : base("TimeSheetContext")
        {
        }
        public DbSet<TimeRecord> TimeRecords { get; set; }
        public DbSet<LeaveApplication> LeaveApplications { get; set; }
        public DbSet<TimeRecordForm> TimeRecordForms { get; set; }
    }
}