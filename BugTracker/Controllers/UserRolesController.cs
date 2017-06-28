using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Data.Entity;
using BugTracker.Models;
using static BugTracker.Controllers.AccountController;

namespace BugTracker.Controllers
{
    [NoDirectAccess]
    [RequireHttps]
    public class UserRolesController : Controller
    {
        private static ApplicationDbContext db = new ApplicationDbContext();
        UserRolesHelper helper = new UserRolesHelper(db);

        //
        // GET: /UserHasRoles/Index
        [Authorize]
        public ActionResult Index()
        {
            List<UserRolesViewModel> userList = new List<UserRolesViewModel>();

            foreach (var user in db.Users.OrderBy(p => p.DisplayName).ToList())
            {
                UserRolesViewModel uservm = new UserRolesViewModel();
                uservm.User = user;
                uservm.SelectedRoles = helper.UserAssignedRoles(user.Id).ToArray();
                userList.Add(uservm);
            }
            return View(userList);
        }

        //
        // GET: /UsersForRole/Index
        [Authorize]
        public ActionResult UsersForRole()
        {
            List<UsersForRoleViewModel> userList = new List<UsersForRoleViewModel>();

            UsersForRoleViewModel uservm1 = new UsersForRoleViewModel();
            uservm1.Role = "Admin";
            uservm1.Users = helper.UsersInRole("Admin").ToArray();
            userList.Add(uservm1);
            UsersForRoleViewModel uservm2 = new UsersForRoleViewModel();
            uservm2.Role = "ProjectManager";
            uservm2.Users = helper.UsersInRole("ProjectManager").ToArray();
            userList.Add(uservm2);
            UsersForRoleViewModel uservm3 = new UsersForRoleViewModel();
            uservm3.Role = "Developer";
            uservm3.Users = helper.UsersInRole("Developer").ToArray();
            userList.Add(uservm3);
            UsersForRoleViewModel uservm4 = new UsersForRoleViewModel();
            uservm4.Role = "Submitter";
            uservm4.Users = helper.UsersInRole("Submitter").ToArray();
            userList.Add(uservm4);

            return View(userList);
        }

        //
        // GET: /UserRoles/Details
        [Authorize]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            UserRolesViewModel uservm = new UserRolesViewModel();
            ApplicationUser user = db.Users.Find(id);
            uservm.User = user;
            TempData["CurrentUser"] = user;
            uservm.SelectedRoles = helper.ListUserRoles(id).ToArray();
            uservm.RolesList = new MultiSelectList(db.Roles.OrderBy(p => p.Name), "Name", "Name", uservm.SelectedRoles);

            if (uservm == null)
            {
                return HttpNotFound();
            }
            return View(uservm);
        }
        
        // POST: UserRoles/Details
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Details([Bind(Include = "SelectedRoles,RolesList")] UserRolesViewModel model)
        {

            if (ModelState.IsValid)
            {
                var user = (ApplicationUser)TempData["CurrentUser"];
                var userRoles = helper.ListUserRoles(user.Id);

                //DELETE current Roles)
                foreach (var roleName in userRoles)
                {
                    helper.RemoveUserFromRole(user.Id, roleName);
                }

                //ADD the whole list of new Roles)
                if (model.SelectedRoles != null)
                {
                    foreach (var roleName in model.SelectedRoles)
                    {
                        helper.AddUserToRole(user.Id, roleName);
                    }
                }

                db.SaveChanges();

            }
            return RedirectToAction("Index");
        }

    }
}