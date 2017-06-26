using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        public async Task<ActionResult> Index()
        {
            if (User.IsInRole("Admin"))
            {
                Debug.WriteLine("In Admin Role");
            }

            if (User.IsInRole("Manager"))
            {
                Debug.WriteLine("In Manager Role");
            }
            ActiveDirectoryClient activeDirectoryClient = UserProfileController.GetActiveDirectoryClient();
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            try
            {
                var result = await activeDirectoryClient.Users
                    .Where(u => u.ObjectId.Equals(userObjectID))
                    .ExecuteAsync();
                IUser user = result.CurrentPage.ToList().First();
                Session["DisplayName"] = user.GivenName;
            }
            catch (AdalException ex)
            {
                throw ex;
            }            
            return View();
        }
    }
}