﻿using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Owin;

namespace TimeSheet
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
        public static bool NoRecords = true;
    }
}
