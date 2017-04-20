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
            List<Holiday> holidayList = db.Holidays.ToList();
            return View(holidayList);
        }


        // GET: Holidays/Update
        public ActionResult Update()
        {
            List<Holiday> holidayList = db.Holidays.ToList();
            foreach(Holiday item in holidayList)
            {
                db.Holidays.Remove(item);
            }
            db.SaveChanges();
            holidayList = PayPeriod.GetHoliday();
            foreach(Holiday item in holidayList)
            {
                db.Holidays.Add(item);
            }
            db.SaveChanges();
            return RedirectToAction("Index");
        }

    }
}
