using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;

namespace TimeSheet.Models
{
    public class AdminDb : DbContext
    {
        public AdminDb() : base("AdminDb")
        {
        }
        public DbSet<Holiday> Holidays { get; set; }
    }
}