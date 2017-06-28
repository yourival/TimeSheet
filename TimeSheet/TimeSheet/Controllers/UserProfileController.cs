using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using TimeSheet.Models;
using System.Collections;

namespace TimeSheet.Controllers
{
    [Authorize]
    public class UserProfileController : Controller
    {
        private ApplicationDb db = new ApplicationDb();
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string graphResourceID = "https://graph.windows.net";

        // GET: UserProfile
        public async Task<ActionResult> Index()
        {
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            try
            {
                ActiveDirectoryClient activeDirectoryClient = GetActiveDirectoryClient();

                // use the token for querying the graph to get the user details

                var result = await activeDirectoryClient.Users
                    .Where(u => u.ObjectId.Equals(userObjectID))
                    .ExecuteAsync();
                IUser user = result.CurrentPage.ToList().First();

                return View(user);
            }
            catch (AdalException)
            {
                // Return to error page.
                return View("Error");
            }
            // if the above failed, the user needs to explicitly re-authenticate for the app to obtain the required token
            catch (Exception)
            {
                return View("Relogin");
            }
        }

        public void RefreshSession()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(
                new AuthenticationProperties { RedirectUri = "/UserProfile" },
                OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        public static async Task<string> GetTokenForApplication()
        {
            string signedInUserID = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;

            // get a token for the Graph without triggering any user interaction (from the cache, via multi-resource refresh token, etc)
            ClientCredential clientcred = new ClientCredential(clientId, appKey);
            // initialize AuthenticationContext with the token cache of the currently signed in user, as kept in the app's database
            AuthenticationContext authenticationContext = new AuthenticationContext(aadInstance + tenantID, new ADALTokenCache(signedInUserID));
            AuthenticationResult authenticationResult = await authenticationContext.AcquireTokenSilentAsync(graphResourceID, clientcred, new UserIdentifier(userObjectID, UserIdentifierType.UniqueId));
            return authenticationResult.AccessToken;
        }

        public static ActiveDirectoryClient GetActiveDirectoryClient()
        {
            string tenantID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;
            string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
            Uri servicePointUri = new Uri(graphResourceID);
            Uri serviceRoot = new Uri(servicePointUri, tenantID);
            ActiveDirectoryClient client = new ActiveDirectoryClient(serviceRoot,
                async () => await GetTokenForApplication());
            return client;
        }

        //public async Task<IUser> GetUserName
        //{
        //    ActiveDirectoryClient activeDirectoryClient = UserProfileController.GetActiveDirectoryClient();
        //    string userObjectID = ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier").Value;
        //    try
        //    {
        //        var result = activeDirectoryClient.Users
        //            .Where(u => u.ObjectId.Equals(userObjectID))
        //            .ExecuteAsync();
        //    IUser user = result.CurrentPage.ToList().First();
        //    Session["displayname"] = user.GivenName;
        //    }
        //    catch (AdalException ex)
        //    {
        //        throw ex;
        //    }
        //    return
        //}
    }
}
