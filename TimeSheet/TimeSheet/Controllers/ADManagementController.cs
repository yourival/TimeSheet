﻿using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services.Client;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace TimeSheet.Controllers
{
    /// <summary>
    ///     Controller to create, edit, and delete AD online user data.
    /// </summary>
    [AuthorizeUser(Roles = "Admin")]
    public class ADManagementController : Controller
    {
        /// GET: ADManagement
        /// <summary>
        ///     Indexe page of ADManagement.
        /// </summary>
        /// <returns></returns>
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        ///     Gets a list of <see cref="User" /> objects that are members of this site's group.
        ///     Notice: They are Azure Directory online <see cref="User" /> data, not <see cref="ADUser" />
        ///     that stored locally.
        /// </summary>
        /// <returns>A view with the list of <see cref="User" /> objects.</returns>
        public async Task<ActionResult> User()
        {
            var userList = new List<User>();
            string NTI_Staff_GroupID = ConfigurationManager.AppSettings["ida:NTI_Staff_GroupID"];
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
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext()
                        .Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View(userList);
            }
            return View(userList);
        }

        /// <summary>
        ///     Gets details of a single <see cref="User" /> Graph.
        /// </summary>
        /// <returns>A view with the details of a single <see cref="User" />.</returns>
        public async Task<ActionResult> UserDetails(string objectId)
        {
            User user = null;
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                user = (User)await client.Users.GetByObjectId(objectId).ExecuteAsync();
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext()
                        .Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            return View(user);
        }

        /// <summary>
        ///     Creates a view to for adding a new <see cref="User" /> to Graph.
        /// </summary>
        /// <returns>A view with the details to add a new <see cref="User" /> objects</returns>
        public async Task<ActionResult> UserCreate()
        {
            return View();
        }

        /// <summary>
        ///     Processes creation of a new <see cref="User" /> to Graph.
        /// </summary>
        /// <returns>A view with the details to all <see cref="User" /> objects</returns>
        [HttpPost]
        public async Task<ActionResult> UserCreate(
            [Bind(
                Include =
                    "UserPrincipalName,AccountEnabled,PasswordProfile,MailNickname,DisplayName,GivenName,Surname,JobTitle,Department"
                )] User user)
        {
            ActiveDirectoryClient client = null;
            try
            {
                client = UserProfileController.GetActiveDirectoryClient();
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext()
                        .Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            try
            {
                await client.Users.AddUserAsync(user);
                return RedirectToAction("User");
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }

        /// <summary>
        ///     Creates a view to for editing an existing <see cref="User" /> in Graph.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User" />.</param>
        /// <returns>A view with details to edit <see cref="User" />.</returns>
        public async Task<ActionResult> UserEdit(string objectId)
        {
            User user = null;
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                user = (User)await client.Users.GetByObjectId(objectId).ExecuteAsync();
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext()
                        .Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            return View(user);
        }

        /// <summary>
        ///     Processes editing of an existing <see cref="User" />.
        /// </summary>
        /// <param name="user"><see cref="User" /> to be edited.</param>
        /// <param name="values">user input from the form</param>
        /// <returns>A view with list of all <see cref="User" /> objects.</returns>
        [HttpPost]
        public async Task<ActionResult> UserEdit(
            User user, FormCollection values)
        {
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                string userId = RouteData.Values["id"].ToString();
                IUser toUpdate = await client.Users.GetByObjectId(userId).ExecuteAsync();
                await toUpdate.UpdateAsync();
                return RedirectToAction("User");
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }

        /// <summary>
        ///     Creates a view to delete an existing <see cref="User" />.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User" />.</param>
        /// <returns>A view of the <see cref="User" /> to be deleted.</returns>
        public async Task<ActionResult> UserDelete(string objectId)
        {
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                var user = (User)await client.Users.GetByObjectId(objectId).ExecuteAsync();
                return View(user);
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }

        /// <summary>
        ///     Processes the deletion of a given <see cref="User" />.
        /// </summary>
        /// <param name="user"><see cref="User" /> to be deleted.</param>
        /// <returns>A view to display all the existing <see cref="User" /> objects.</returns>
        [HttpPost]
        public async Task<ActionResult> UserDelete(User user)
        {
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IUser toDelete = await client.Users.GetByObjectId(user.ObjectId).ExecuteAsync();
                await toDelete.DeleteAsync();
                return RedirectToAction("User");
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View(user);
            }
        }

        /// <summary>
        ///     Gets a list of <see cref="User" /> objects that a given <see cref="User" /> has as a direct report.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User" />.</param>
        /// <returns>A view with the list of <see cref="User" /> objects.</returns>
        public async Task<ActionResult> UserGetDirectReports(string objectId)
        {
            List<User> reports = new List<User>();
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IUser user = await client.Users.GetByObjectId(objectId).ExecuteAsync();
                var userFetcher = user as IUserFetcher;
                IPagedCollection<IDirectoryObject> directReports = await userFetcher.DirectReports.ExecuteAsync();
                do
                {
                    List<IDirectoryObject> directoryObjects = directReports.CurrentPage.ToList();
                    foreach (IDirectoryObject directoryObject in directoryObjects)
                    {
                        if (directoryObject is User)
                        {
                            reports.Add((User)directoryObject);
                        }
                    }
                    directReports = await directReports.GetNextPageAsync();
                } while (directReports != null);
            }
            catch (Exception e)
            {
                if (Request.QueryString["reauth"] == "True")
                {
                    //
                    // Send an OpenID Connect sign-in request to get a new set of tokens.
                    // If the user still has a valid session with Azure AD, they will not be prompted for their credentials.
                    // The OpenID Connect middleware will return to this controller after the sign-in response has been handled.
                    //
                    HttpContext.GetOwinContext()
                        .Authentication.Challenge(OpenIdConnectAuthenticationDefaults.AuthenticationType);
                }

                //
                // The user needs to re-authorize.  Show them a message to that effect.
                //
                ViewBag.ErrorMessage = "AuthorizationRequired";
                return View();
            }

            return View(reports);
        }
    }
}

