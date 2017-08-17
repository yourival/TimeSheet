using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using FluentScheduler;
using System.Web.Hosting;
using System.IO;

namespace TimeSheet.Models
{
    public class ScheduledJob : IJob, IRegisteredObject
    {
        private readonly object _lock = new object();

        private bool _shuttingDown;

        public ScheduledJob()
        {
            // Register this job with the hosting environment.
            // Allows for a more graceful stop of the job, in the case of IIS shutting down.
            HostingEnvironment.RegisterObject(this);
        }

        public void Execute()
        {
            lock (_lock)
            {
                if (_shuttingDown)
                    return;


                // Keep tracks. Should be removed for production
                File.AppendAllText(@"C:\Downloads\test.txt", DateTime.Now + Environment.NewLine);
            }
        }

        public void Stop(bool immediate)
        {
            // Locking here will wait for the lock in Execute to be released until this code can continue.
            lock (_lock)
            {
                _shuttingDown = true;
            }

            HostingEnvironment.UnregisterObject(this);
        }
    }
}