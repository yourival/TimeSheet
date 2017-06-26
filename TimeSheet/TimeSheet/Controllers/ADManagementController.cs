using Microsoft.Azure.ActiveDirectory.GraphClient;
using Microsoft.Azure.ActiveDirectory.GraphClient.Extensions;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace TimeSheet.Controllers
{
    [AuthorizeUser(Roles = "Manager")]
    public class ADManagementController : Controller
    {
        // GET: ADManagement
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> User()
        {
            var userList = new List<User>();
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IPagedCollection<IUser> pagedCollection = await client.Users.ExecuteAsync();
                if (pagedCollection != null)
                {
                    do
                    {
                        List<IUser> usersList = pagedCollection.CurrentPage.ToList();
                        foreach (IUser user in usersList)
                        {
                            userList.Add((User)user);
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
        ///     Gets a list of <see cref="Group" /> objects that a given <see cref="User" /> is member of.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="User" />.</param>
        /// <returns>A view with the list of <see cref="Group" /> objects.</returns>
        public async Task<ActionResult> UserGetGroups(string objectId)
        {
            IList<Group> groupMembership = new List<Group>();

            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();

                IUser user = await client.Users.GetByObjectId(objectId).ExecuteAsync();
                var userFetcher = (IUserFetcher)user;

                IPagedCollection<IDirectoryObject> pagedCollection = await userFetcher.MemberOf.ExecuteAsync();
                do
                {
                    List<IDirectoryObject> directoryObjects = pagedCollection.CurrentPage.ToList();
                    foreach (IDirectoryObject directoryObject in directoryObjects)
                    {
                        if (directoryObject is Group)
                        {
                            var group = directoryObject as Group;
                            groupMembership.Add(group);
                        }
                    }
                    pagedCollection = await pagedCollection.GetNextPageAsync();
                } while (pagedCollection != null);
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

            return View(groupMembership);
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

        /// <summary>
        ///     Gets a list of <see cref="Group" /> objects from Graph.
        /// </summary>
        /// <returns>A view with the list of <see cref="Group" /> objects.</returns>
        public async Task<ActionResult> Group()
        {
            List<Group> groupList = new List<Group>();

            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IPagedCollection<IGroup> pagedCollection = await client.Groups.ExecuteAsync();
                do
                {
                    List<IGroup> groups = pagedCollection.CurrentPage.ToList();
                    foreach (IGroup group in groups)
                    {
                        groupList.Add((Group)group);
                    }
                    pagedCollection = pagedCollection.GetNextPageAsync().Result;
                } while (pagedCollection != null);
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
                return View(groupList);
            }
            return View(groupList);
        }

        /// <summary>
        ///     Gets details of a single <see cref="Group" /> Graph.
        /// </summary>
        /// <returns>A view with the details of a single <see cref="Group" />.</returns>
        public async Task<ActionResult> GroupDetails(string objectId)
        {
            Group group = null;
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                group = (Group)await client.Groups.GetByObjectId(objectId).ExecuteAsync();
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

            return View(group);
        }

        /// <summary>
        ///     Creates a view to for adding a new <see cref="Group" /> to Graph.
        /// </summary>
        /// <returns>A view with the details to add a new <see cref="Group" /> objects</returns>
        public async Task<ActionResult> GroupCreate()
        {
            return View();
        }

        /// <summary>
        ///     Processes creation of a new <see cref="Group" /> to Graph.
        /// </summary>
        /// <param name="group"><see cref="Group" /> to be created.</param>
        /// <returns>A view with the details to all <see cref="Group" /> objects</returns>
        [HttpPost]
        public async Task<ActionResult> GroupCreate(
            [Bind(Include = "DisplayName,Description,MailNickName,SecurityEnabled")] Group group)
        {
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                await client.Groups.AddGroupAsync(group);
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
            ViewBag.ErrorMessage = "AuthorizationRequired";
            return View();
        }


        /// <summary>
        ///     Creates a view to for editing an existing <see cref="Group" /> in Graph.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="Group" />.</param>
        /// <returns>A view with details to edit <see cref="Group" />.</returns>
        public async Task<ActionResult> GroupEdit(string objectId)
        {
            Group group = null;
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                group = (Group)await client.Groups.GetByObjectId(objectId).ExecuteAsync();
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
            return View(group);
        }

        /// <summary>
        ///     Processes editing of an existing <see cref="Group" />.
        /// </summary>
        /// <param name="group"><see cref="Group" /> to be edited.</param>
        /// <returns>A view with list of all <see cref="Group" /> objects.</returns>
        [HttpPost]
        public async Task<ActionResult> GroupEdit(Group group, FormCollection values)
        {
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IGroup toUpdate = await client.Groups.GetByObjectId(group.ObjectId).ExecuteAsync();
                await toUpdate.UpdateAsync();
                return RedirectToAction("Index");
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }


        /// <summary>
        ///     Creates a view to delete an existing <see cref="Group" />.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="Group" />.</param>
        /// <returns>A view of the <see cref="Group" /> to be deleted.</returns>
        public async Task<ActionResult> GroupDelete(string objectId)
        {
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                Group group = (Group)await client.Groups.GetByObjectId(objectId).ExecuteAsync();
                return View(group);
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View();
            }
        }

        /// <summary>
        ///     Processes the deletion of a given <see cref="Group" />.
        /// </summary>
        /// <param name="group"><see cref="Group" /> to be deleted.</param>
        /// <returns>A view to display all the existing <see cref="Group" /> objects.</returns>
        [HttpPost]
        public async Task<ActionResult> GroupDelete(Group group)
        {
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IGroup toDelete = await client.Groups.GetByObjectId(@group.ObjectId).ExecuteAsync();
                await toDelete.DeleteAsync();
                return RedirectToAction("Index");
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.Message);
                return View(group);
            }
        }

        /// <summary>
        ///     Gets a list of <see cref="Group" /> objects that a given <see cref="Group" /> is member of.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="Group" />.</param>
        /// <returns>A view with the list of <see cref="Group" /> objects.</returns>
        public async Task<ActionResult> GroupGetGroups(string objectId)
        {
            IList<Group> groupMemberShip = new List<Group>();

            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IGroup group = await client.Groups.GetByObjectId(objectId).ExecuteAsync();
                IGroupFetcher groupFetcher = group as IGroupFetcher;
                IPagedCollection<IDirectoryObject> pagedCollection = await groupFetcher.MemberOf.ExecuteAsync();
                do
                {
                    List<IDirectoryObject> directoryObjects = pagedCollection.CurrentPage.ToList();
                    foreach (IDirectoryObject directoryObject in directoryObjects)
                    {
                        if (directoryObject is Group)
                        {
                            groupMemberShip.Add((Group)directoryObject);
                        }
                    }
                    pagedCollection = await pagedCollection.GetNextPageAsync();
                } while (pagedCollection != null);
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

            return View(groupMemberShip);
        }

        /// <summary>
        ///     Gets a list of <see cref="User" /> objects that are members of a give <see cref="Group" />.
        /// </summary>
        /// <param name="objectId">Unique identifier of the <see cref="Group" />.</param>
        /// <returns>A view with the list of <see cref="User" /> objects.</returns>
        public async Task<ActionResult> GroupGetMembers(string objectId)
        {
            IList<User> users = new List<User>();
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IGroup group = await client.Groups.GetByObjectId(objectId).ExecuteAsync();
                IGroupFetcher groupFetcher = group as IGroupFetcher;
                IPagedCollection<IDirectoryObject> pagedCollection = await groupFetcher.Members.ExecuteAsync();
                do
                {
                    List<IDirectoryObject> directoryObjects = pagedCollection.CurrentPage.ToList();
                    foreach (IDirectoryObject directoryObject in directoryObjects)
                    {
                        if (directoryObject is User)
                        {
                            users.Add((User)directoryObject);
                        }
                    }
                    pagedCollection = await pagedCollection.GetNextPageAsync();
                } while (pagedCollection != null);
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

            return View(users);
        }

        /// <summary>
        ///     Gets a list of <see cref="Role" /> objects from Graph.
        /// </summary>
        /// <returns>A view with the list of <see cref="Role" /> objects.</returns>
        public async Task<ActionResult> Role()
        {
            List<DirectoryRole> roleList = new List<DirectoryRole>();

            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                IPagedCollection<IDirectoryRole> pagedCollection = await client.DirectoryRoles.ExecuteAsync();
                do
                {
                    List<IDirectoryRole> directoryRoles = pagedCollection.CurrentPage.ToList();
                    foreach (IDirectoryRole directoryRole in directoryRoles)
                    {
                        roleList.Add((DirectoryRole)directoryRole);
                    }
                    pagedCollection = await pagedCollection.GetNextPageAsync();
                } while (pagedCollection != null);
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
                return View(roleList);
            }
            return View(roleList);
        }

        /// <summary>
        ///     Gets details of a single <see cref="Role" /> Graph.
        /// </summary>
        /// <returns>A view with the details of a single <see cref="Role" />.</returns>
        public async Task<ActionResult> RoleDetails(string objectId)
        {
            DirectoryRole role = null;
            try
            {
                ActiveDirectoryClient client = UserProfileController.GetActiveDirectoryClient();
                role = (DirectoryRole)await client.DirectoryRoles.GetByObjectId(objectId).ExecuteAsync();
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

            return View(role);
        }
    }
}

