using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IdentityModel.Claims;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using TimeSheet.Models;
using FluentScheduler;
using System.Diagnostics;

namespace TimeSheet
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // Seed the database with sample data for development. This code should be removed for production.
            Database.SetInitializer<TimeSheetDb>(new TimeSheetInitializer());
            Database.SetInitializer<AdminDb>(new AdminDbInitializer());

            // Register schedule events
            var registry = new Registry();
            registry.Schedule<ScheduledJob>().ToRunEvery(1).Weeks().On(DayOfWeek.Tuesday).At(19, 0);
            JobManager.Initialize(registry);

            // Configure application
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;
        }

        protected void Application_End()
        {
            JobManager.Stop();
        }
    }
}
