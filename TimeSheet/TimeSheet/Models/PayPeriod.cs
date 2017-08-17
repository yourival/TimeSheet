using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace TimeSheet.Models
{
    public static class PayPeriod
    {
        static DateTime FirstPayPeriod = new DateTime(2017, 1, 10);

        public static DateTime FirstPayDayOfYear(int year)
        {
            DateTime newYear = new DateTime(year, 1, 1);
            //example: get the first day of Tuesday: 2 + 7 - DayOfWeek of new year
             DateTime firstTuesday = newYear.AddDays((9 - (int)newYear.DayOfWeek) % 7);

            //retrun first pay day of the year
            if ((firstTuesday - FirstPayPeriod).Days % 14 == 0)
                return firstTuesday;
            else
                return firstTuesday.AddDays(7);
        }

        // number of periods in the year
        public static int GetPeriodAmount(int year)
        {
            return (FirstPayDayOfYear(year + 1) - FirstPayDayOfYear(year)).Days / 14;
        }

        // the peroid number of a specific date
        public static int GetPeriodNum(DateTime date)
        {
            int days = (date - (FirstPayDayOfYear(date.Year)).AddDays(-13)).Days;
            return days / 14 + 1;
        }

        public static DateTime GetEndDay(int year, int period)
        {
            return FirstPayDayOfYear(year).AddDays(14 * (period - 1));
        }

        public static DateTime GetStartDay(int year, int period)
        {
            return GetEndDay(year, period).AddDays(-13);
        }

        //get dropdown list for year, allow user to select upto 5 years
        public static List<SelectListItem> GetYearItems()
        {
            List<SelectListItem> listItems = new List<SelectListItem>();
            for (int i = 0; i < 5; i++)
            {
                int year = DateTime.Now.Year + i;
                SelectListItem newItem = new SelectListItem
                {
                    Text = year.ToString(),
                    Value = year.ToString()
                };

                if (i == 0)
                    newItem.Selected = true;

                listItems.Add(newItem);
            }
            return listItems;
        }

        //get dropdown list for pay period
        public static List<SelectListItem> GetPeriodItems(int year)
        {
            List<SelectListItem> listItems = new List<SelectListItem>();
            int currentPeriod = GetPeriodNum(DateTime.Now);
            int periodAmount = GetPeriodAmount(year);
            for (int i = 0; i < periodAmount; i++)
            {
                int period = i + 1;
                DateTime start = GetStartDay(year, period);
                DateTime end = GetEndDay(year, period); 
                SelectListItem newItem = new SelectListItem
                {
                    Text = period.ToString() +
                           String.Format(" ({0:dd/MM} - {1:dd/MM})", start, end),
                    Value = (i + 1).ToString()
                };

                if (i == currentPeriod || (currentPeriod == periodAmount && i == 0))
                    newItem.Selected = true;

                listItems.Add(newItem);
            }
            return listItems;
        }

        // Set holiday status for TimeRecords
        public static void SetPublicHoliday(TimeRecord record)
        {
            AdminDb adminDb = new AdminDb();
            List<Holiday> holidayLists = adminDb.Holidays.ToList();
            if (holidayLists.Count != 0)
            {
                foreach (Holiday holiday in holidayLists)
                {
                    if (holiday.HolidayDate.Date == record.RecordDate.Date ||
                        record.RecordDate.DayOfWeek == DayOfWeek.Saturday ||
                        record.RecordDate.DayOfWeek == DayOfWeek.Sunday)
                    {
                        record.IsHoliday = true;
                    }
                }

            }
        }

        // Get holiday list from online source
        public static List<Holiday> GetHoliday()
        {
            String RequestString = "http://data.gov.au/api/action/datastore_search_sql?sql=SELECT \"Date\", \"HolidayName\" from \"31eec35e-1de6-4f04-9703-9be1d43d405b\" WHERE \"ApplicableTo\" LIKE '%NSW%' OR \"ApplicableTo\" LIKE 'NAT'";
            List<Holiday> holidayList = new List<Holiday>();
            using (WebClient webClient = new System.Net.WebClient())
            {
                WebClient n = new WebClient();
                var json = n.DownloadString(RequestString);
                var jo = JObject.Parse(json);
                var jsonRecords= jo["result"]["records"].ToString();
                //Debug.WriteLine(jsonRecords);
                List<Dictionary<string,string>> results = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(jsonRecords);
                foreach (Dictionary<string,string> item in results)
                {
                    DateTime holiday = DateTime.ParseExact(item["Date"], "yyyyMMdd", CultureInfo.InvariantCulture);//convert string to datetime
                    holidayList.Add(new Holiday { HolidayDate = holiday, HolidayName =  item["HolidayName"] });
                }
            }
            return holidayList;
        }

        static public void UpdateLeaveBalance()
        {
            // Only updates when it is the end day of a pay period
            if ((DateTime.Now - FirstPayPeriod).Days % 14 == 0)
            {
                using (TimeSheetDb context = new TimeSheetDb())
                {
                    const double auunalAccuralRate = 0.076923;
                    const double sickAccuralRate = 0.038462;
                    _leaveType leaveType;
                    double rate;
                    
                    List<ADUser> users = context.ADUsers.Where(u => u.ContractHours != 0).ToList();

                    foreach(var user in users)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            if(i == 0)
                            {
                                leaveType = _leaveType.annual;
                                rate = auunalAccuralRate;
                            }
                            else
                            {
                                leaveType = _leaveType.sick;
                                rate = sickAccuralRate;
                            }

                            // Update leaves balances
                            LeaveBalance balance = context.LeaveBalances.Find(user.Email, leaveType);
                            if (balance == null)
                            {
                                balance = new LeaveBalance
                                {
                                    UserID = user.Email,
                                    UserName = user.UserName,
                                    LeaveType = leaveType,
                                    AvailableLeaveHours = 0.0
                                };
                                context.LeaveBalances.Add(balance);
                            }
                            else
                            {
                                balance.AvailableLeaveHours += user.ContractHours * rate;
                                context.Entry(balance).State = EntityState.Modified;
                            }
                        }                        
                    }
                    context.SaveChanges();
                }
            }
        }
    }
}