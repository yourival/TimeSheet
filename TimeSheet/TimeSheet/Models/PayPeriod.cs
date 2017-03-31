using System;
using System.Collections.Generic;
using System.Web.Mvc;
using System.Linq;
using System.Web;

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
    }
}