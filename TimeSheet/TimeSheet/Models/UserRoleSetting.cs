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
        public enum _worktype {
            [Display(Name = "Full Time")]
            fulltime,
            [Display(Name = "Part Time")]
            parttime,
            [Display(Name = "Casual")]
            casual
        };
        [Key]
        public int id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "Manager ID")]
        public String UserID { get; set; }

        [Required]
        [Display(Name = "Manager Name")]
        public String UserName { get; set; }

        [Display(Name = "Administrator")]
        public bool IsAdmin { get; set; }

        [Display(Name = "Accountant")]
        public bool IsAccountant { get; set; }

        [Display(Name = "Manager")]
        public bool IsManager { get; set; }

        [Display(Name = "Work Type")]
        public _worktype WorkType { get; set; }

        public static List<SelectListItem> GetManagerItems()
        {
            AdminDb adminDb = new AdminDb();
            List<SelectListItem> listItems = new List<SelectListItem>();
            List<UserRoleSetting> managerList = adminDb.UserRoleSettings.Where(u => u.IsManager).ToList();
            for (int i = 0; i < managerList.Count; i++)
            {
                listItems.Add(new SelectListItem
                {
                    Text = managerList[i].UserName,
                    Value = managerList[i].UserID
                });
            }
            if(listItems.Count != 0)
                listItems.First().Selected = true;

            return listItems;
        }
    }
}