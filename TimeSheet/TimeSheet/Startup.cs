using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;
using TimeSheet.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace TimeSheet
{
    public partial class Startup
    {
        //check if the first time to access the timesheet page
        public static bool NoRecords = true;

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        private void CreateRoles()
        {
            ApplicationDb context = new ApplicationDb();
            AdminDb adminDb = new AdminDb();
            var roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));

            if (!roleManager.RoleExists("Admin"))
            {
                var role = new IdentityRole();
                role.Name = "Admin";
                roleManager.Create(role);
            }
        }

    }
}
