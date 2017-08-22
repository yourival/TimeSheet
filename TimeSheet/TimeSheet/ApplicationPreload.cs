using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentScheduler;
using TimeSheet.Models;

namespace TimeSheet
{
    /* This warm-up will be implemented every time when server restarts or 
       the application pool is recycled */
    public class ApplicationPreload : System.Web.Hosting.IProcessHostPreloadClient
    {
        public void Preload(string[] parameters)
        {
            var registry = new Registry();
            registry.Schedule<ScheduledJob>().ToRunEvery(0).Weeks().On(DayOfWeek.Tuesday).At(19,0);
            JobManager.Initialize(registry);
        }
    }
}