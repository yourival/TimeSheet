using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public string username;
        public ActionResult Index()
        {
            TimeSheetDb contextDb = new TimeSheetDb();
            if (contextDb.ADUsers.Count() == 0)
                ADUser.GetADUser();

            return View();
        }

        public async Task<ActionResult> LoginLayout()
        {
            if (username == null)
            {
                ActiveDirectoryClient activeDirectoryClient = UserProfileController.GetActiveDirectoryClient();
                string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
                try
                {
                    var result = await activeDirectoryClient.Users
                        .Where(u => u.ObjectId.Equals(userObjectID))
                        .ExecuteAsync();
                    IUser user = result.CurrentPage.ToList().First();
                    username = user.GivenName;
                }
                catch (AdalException ex)
                {
                    throw ex;
                }
            }

            return PartialView("_LoginPartial", username);
        }
    }
}