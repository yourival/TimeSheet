using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TimeSheet.Models
{
    public class UserRoleSetting
    {
        [Key]
        public int id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Manager ID")]
        public String UserID { get; set; }

        [Required]
        [Display(Name = "Manager Name")]
        public String UserName { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsAccountant { get; set; }

        public bool IsManager { get; set; }

        public static List<SelectListItem> GetManagerItems()
        {
            AdminDb adminDb = new AdminDb();
            List<SelectListItem> listItems = new List<SelectListItem>();
            List<UserRoleSetting> managerList = adminDb.UserRoleSettings.Where(u => u.IsManager).ToList();
            for (int i = 0; i < managerList.Count(); i++)
            {
                if (i == 0)
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = managerList[i].UserName,
                        Value = managerList[i].UserID,
                        Selected = true
                    });
                }
                else
                {
                    listItems.Add(new SelectListItem
                    {
                        Text = managerList[i].UserName,
                        Value = managerList[i].UserID
                    });
                }
            }
            return listItems;
        }
    }
}