using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TimeSheet.Models
{
    public class Manager
    {
        [Key]
        public int id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Manager ID")]
        public String ManagerID { get; set; }

        [Required]
        [Display(Name = "Manager Name")]
        public String ManagerName { get; set; }

        public bool IsAdmin { get; set; }

        public static List<SelectListItem> GetManagerItems()
        {
            AdminDb adminDb = new AdminDb();
            List<SelectListItem> listItems = new List<SelectListItem>();
            List<Manager> managerList = adminDb.ManagerSetting.ToList();
            for (int i = 0; i < managerList.Count(); i++)
            {
                if (i == 0)
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = managerList[i].ManagerName,
                        Value = managerList[i].ManagerID,
                        Selected = true
                    });
                }
                else
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = managerList[i].ManagerName,
                        Value = managerList[i].ManagerID
                    });
                }
            }
            return listItems;
        }
    }
}