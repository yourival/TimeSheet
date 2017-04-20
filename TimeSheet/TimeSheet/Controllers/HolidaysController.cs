using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class HolidaysController : Controller
    {
        private AdminDb db = new AdminDb();

        // GET: Holidays
        public ActionResult Index()
        {
            return View();
        }


        // GET: Holidays/Update
        public ActionResult Update()
        {
            return View();
        }

    }
}
