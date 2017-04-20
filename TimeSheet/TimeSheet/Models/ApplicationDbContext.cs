using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace TimeSheet.Models
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
            : base("DefaultConnection")
        {
        }

        public DbSet<UserTokenCache> UserTokenCacheList { get; set; }
        //public DbSet<PayForm> PayFormList { get; set; }
    }

    public class UserTokenCache
    {
        [Key]
        public int UserTokenCacheId { get; set; }
        public string webUserUniqueId { get; set; }
        public byte[] cacheBits { get; set; }
        public DateTime LastWrite { get; set; }
    }

    //public class PayForm
    //{
    //    public int PayFormId { get; set; }
    //    public int UserId { get; set; }
    //    public int ManagerId { get; set; }
    //    public DateTime Pay { get; set; }
    //    public string Comments { get; set; }
    //}
}
