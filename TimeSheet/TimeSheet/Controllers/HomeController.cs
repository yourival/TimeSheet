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
    /// <summary>
    ///     A countroller handling homepage generating and user login page.
    /// </summary>
    [Authorize]
    public class HomeController : Controller
    {
        /// <summary>
        ///     The user name for current user
        /// </summary>
        static public string username;
        static private string userid = string.Empty;

        /// <summary>
        ///     Create a Homepage for the website.
        /// </summary>
        /// <returns>Homepage of the website with layout and navigation bar.</returns>
        // GET: LeaveApplication
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        ///     Display personalised master page.
        /// </summary>
        /// <returns>A partial view of a link to personal profile with user name</returns>
        public async Task<ActionResult> LoginLayout()
        {
            if (userid != User.Identity.Name || username == null)
            {
                userid = User.Identity.Name;
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