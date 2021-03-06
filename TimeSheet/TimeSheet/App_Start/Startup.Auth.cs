﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Owin;
using TimeSheet.Models;
using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Owin.Security.Notifications;
using Microsoft.IdentityModel.Protocols;

namespace TimeSheet
{
    public partial class Startup
    {
        private static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        private static string appKey = ConfigurationManager.AppSettings["ida:ClientSecret"];
        private static string aadInstance = ConfigurationManager.AppSettings["ida:AADInstance"];
        private static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        private static string postLogoutRedirectUri = ConfigurationManager.AppSettings["ida:PostLogoutRedirectUri"];

        public static readonly string Authority = aadInstance + tenantId;

        // This is the resource ID of the AAD Graph API.  We'll need this to request a token to call the Graph API.
        string graphResourceId = "https://graph.windows.net";

        public void ConfigureAuth(IAppBuilder app)
        {
            ApplicationDb db = new ApplicationDb();

            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);
            app.UseKentorOwinCookieSaver();
            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = clientId,
                    Authority = Authority,
                    PostLogoutRedirectUri = postLogoutRedirectUri,

                    TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    {
                        // map the claimsPrincipal's roles to the roles claim
                        RoleClaimType = "roles",
                    },

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        // If there is a code in the OpenID Connect response, redeem it for an access token and refresh token, and store those away.

                        RedirectToIdentityProvider = ctx =>
                        {
                            bool isAjaxRequest = (ctx.Request.Headers != null && ctx.Request.Headers["X-Requested-With"] == "XMLHttpRequest");

                            if (isAjaxRequest)
                            {
                                ctx.Response.Headers.Remove("Set-Cookie");
                                ctx.State = NotificationResultState.HandledResponse;
                            }

                            return Task.FromResult(0);
                        },
                       AuthorizationCodeReceived = (context) =>
                       {
                           var code = context.Code;
                           ClientCredential credential = new ClientCredential(clientId, appKey);
                           string signedInUserID = context.AuthenticationTicket.Identity.FindFirst(ClaimTypes.NameIdentifier).Value;
                           AuthenticationContext authContext = new AuthenticationContext(Authority, new ADALTokenCache(signedInUserID));
                           AuthenticationResult result = authContext.AcquireTokenByAuthorizationCode(
                           code, new Uri(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path)), credential, graphResourceId);

                           // Set up user roles
                           UserRoleSetting userRole = AADHelper.GetUserRole(context.AuthenticationTicket.Identity.Name);
                           if(userRole != null)
                           {
                               if (userRole.IsAdmin)
                               {
                                   context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "Admin"));
                                   context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "Manager"));
                               }
                               else
                               {
                                   if (userRole.IsManager)
                                   {
                                       context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "Manager"));
                                   }
                                   if (userRole.IsAccountant)
                                   {
                                       context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "Accountant"));
                                   }
                               }
                               switch (userRole.WorkType)
                               {
                                   case UserRoleSetting._worktype.fulltime:
                                       context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "FullTimeWorker"));
                                       break;
                                   case UserRoleSetting._worktype.parttime:
                                       context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "PartTimeWorker"));
                                       break;
                                   case UserRoleSetting._worktype.casual:
                                       context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "CasualWorker"));
                                       break;
                                   default:
                                       context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "FullTimeWorker"));
                                       break;
                               }
                           }
                           else
                           {
                               context.AuthenticationTicket.Identity.AddClaim(new Claim("roles", "FullTimeWorker"));
                           }

                           return Task.FromResult(0);
                       }
                    }
            });
        }
    }
}
