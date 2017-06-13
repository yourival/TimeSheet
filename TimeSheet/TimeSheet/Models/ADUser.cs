using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using TimeSheet.Controllers;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using System.Configuration;
using System.Threading.Tasks;

namespace TimeSheet.Models
{
    public class ADUser
    {
        [Key]
        public int id { get; set; }

        public string Email { get; set; }

        public string UserName { get; set; }

        public string JobCode { get; set; }

        public int EmployeeID { get; set; }

        public virtual ICollection<TimeRecord> TimeRecords { get; set; }

        public static async Task GetADUser()
        {
            TimeSheetDb timesheetDb = new TimeSheetDb();
            string NTI_Staff_GroupID = ConfigurationManager.AppSettings["ida:NTI_Staff_GroupID"];
            var userList = new List<User>();
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IGroup group = await client.Groups.GetByObjectId(NTI_Staff_GroupID).ExecuteAsync();
                IGroupFetcher groupFetcher = group as IGroupFetcher;
                IPagedCollection<IDirectoryObject> pagedCollection = await groupFetcher.Members.ExecuteAsync();
                if (pagedCollection != null)
                {
                    do
                    {
                        List<IDirectoryObject> directoryObjects = pagedCollection.CurrentPage.ToList();
                        foreach (IDirectoryObject directoryObject in directoryObjects)
                        {
                            if (directoryObject is User)
                            {
                                userList.Add((User)directoryObject);
                            }
                        }
                        pagedCollection = await pagedCollection.GetNextPageAsync();
                    } while (pagedCollection != null);
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            if (userList != null)
            {
                List<ADUser> SystemUserList = timesheetDb.ADUsers.ToList();
                if (SystemUserList.Count != 0)
                {
                    foreach (var item in SystemUserList)
                    {
                        timesheetDb.ADUsers.Remove(item);
                    }
                    timesheetDb.SaveChanges();
                }
                foreach (var item in userList)
                {
                    ADUser user = new ADUser();
                    user.UserName = item.DisplayName;
                    user.Email = item.Mail;
                    user.JobCode = item.JobTitle;
                    timesheetDb.ADUsers.Add(user);
                    timesheetDb.SaveChanges();
                }
            }
        }
    }
}