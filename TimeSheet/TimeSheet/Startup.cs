using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

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

    }
}
