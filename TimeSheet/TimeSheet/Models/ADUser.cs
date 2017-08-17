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
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int id { get; set; }

        [Key, Column(Order = 0)]
        public string Email { get; set; }

        public string UserName { get; set; }

        public string JobCode { get; set; }

        public string Department { get; set; }

        public string EmployeeID { get; set; }

        public double ContractHours { get; set; }

        //public string Manager { get; set; }

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
                                var user = (User)directoryObject;
                                userList.Add(user);
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
                if (SystemUserList.Count != userList.Count)
                {
                    foreach (var item in userList)
                    {
                        if (timesheetDb.ADUsers.Find(item.Mail) == null)
                            timesheetDb.ADUsers.Add(new ADUser {
                                UserName = item.DisplayName,
                                Email = item.Mail,
                                JobCode = item.JobTitle,
                                Department = item.Department
                            }
                        );
                    }

                    timesheetDb.SaveChanges();
                }
            }
        }
    }
}