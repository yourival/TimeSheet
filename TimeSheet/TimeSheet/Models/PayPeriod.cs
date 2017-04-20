using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

        public static int GetPeriodAmount(int year)
        {
            //number of periods in the year
            return (FirstPayDayOfYear(year + 1) - FirstPayDayOfYear(year)).Days / 14;
        }

        public static DateTime GetEndDay(int year, int period)
        {
            return FirstPayDayOfYear(year).AddDays(14 * (period - 1));
        }

        public static DateTime GetStartDay(int year, int period)
        {
            return GetEndDay(year, period).AddDays(-13);
        }

        //get dropdown list for year, allow user to select upto 3 years
        public static List<SelectListItem> GetYearItems()
        {
            List<SelectListItem> listItems = new List<SelectListItem>();
            for (int i = 0; i < 15; i++)
            {
                if ( i==0 )
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = DateTime.Now.Year.ToString(),
                        Value = DateTime.Now.Year.ToString(),
                        Selected = true
                    });
                }
                else
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = (DateTime.Now.Year+i).ToString(),
                        Value = (DateTime.Now.Year+i).ToString()
                    });
                }
                
            }

            return listItems;
        }

        //get dropdown list for pay period
        public static List<SelectListItem> GetPeriodItems(int year)
        {
            List<SelectListItem> listItems = new List<SelectListItem>();
            for (int i = 0; i < GetPeriodAmount(year); i++)
            {
                if (i == (int)(DateTime.Now - FirstPayDayOfYear(year)).Days/14 + 1)
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = (i + 1).ToString(),
                        Value = (i + 1).ToString(),
                        Selected = true
                    });
                }
                else
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = (i + 1).ToString(),
                        Value = (i + 1).ToString()
                    });
                }
            }
            return listItems;
        }

        public static void SetPublicHoliday(List<TimeRecord> records)
        {
            List<Holiday> holidayLists = GetHoliday();
            DateTime startDate = records.First().StartTime;
            DateTime endDate = records.Last().EndTime;
            foreach (Holiday holiday in holidayLists)
            {
                foreach (TimeRecord record in records)
                {
                    if (holiday.HolidayDate.Date == record.StartTime.Date)
                    {
                        record.isHoliday = true;
                    }
                    if ((int)record.StartTime.DayOfWeek == 6 || (int)record.StartTime.DayOfWeek == 7)
                    {
                        record.isHoliday = true;
                    }
                }
            }
        }

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

    }
}