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
    public class UserAssignmentsController : Controller
    {
        private static ApplicationDbContext db = new ApplicationDbContext();
        ProjectAssignHelper projecthelper = new ProjectAssignHelper();
        UserRolesHelper rolehelper = new UserRolesHelper(db);

        //
        // GET: /Projects,Roles ForUser/Index
        [Authorize]
        public ActionResult Index()
        {

            List<UserAssignmentsViewModel> userList = new List<UserAssignmentsViewModel>();

            foreach (var user in db.Users.OrderBy(p => p.DisplayName).ToList())
            {
                UserAssignmentsViewModel uservm = new UserAssignmentsViewModel();
                uservm.User = user;

                // Work on Project Assignments
                uservm.Projects = projecthelper.ListProjectsForUser(user.Id);
            
                // Work on Role Assignments
                uservm.SelectedRoles = rolehelper.UserAssignedRoles(user.Id).ToArray();

                userList.Add(uservm);
            }

            return View(userList);
        }

        //
        // GET: /Projects,Roles ForUser/Details
        [Authorize]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            UserAssignmentsViewModel uservm = new UserAssignmentsViewModel();
            ApplicationUser user = db.Users.Find(id);
            uservm.User = user;
            TempData["CurrentUser"] = user;

            // Work on Project Assignments
            uservm.Projects = projecthelper.ListProjectsForUser(id);
            uservm.SelectedProjects = uservm.Projects.Select(p => p.Id).ToArray();
            uservm.ProjectsList = new MultiSelectList(db.Projects.OrderBy(p => p.Name), "Id", "Name", uservm.SelectedProjects);

            // Work on Role Assignments
            uservm.SelectedRoles = rolehelper.ListUserRoles(id).ToArray();
            uservm.RolesList = new MultiSelectList(db.Roles.OrderBy(p => p.Name), "Name", "Name", uservm.SelectedRoles);

            if (uservm == null)
            {
                return HttpNotFound();
            }

            return View(uservm);
        }

        // POST: Projects,Roles ForUser/Details
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Details([Bind(Include = "SelectedProjects,ProjectsList,SelectedRoles,RolesList")] UserAssignmentsViewModel model)   // Work on Both Assignments
        {

            if (ModelState.IsValid)
            {
                var user = (ApplicationUser)TempData["CurrentUser"];

                // Work on Project Assignments
                var userProjects = projecthelper.ListProjectsForUser(user.Id);

                //DELETE current Projects)
                foreach (var project in userProjects)
                {
                    projecthelper.RemoveUserFromProject(user.Id, project.Id);
                }

                //ADD the whole list of new Projects)
                if (model.SelectedProjects != null)
                {
                    foreach (var projectid in model.SelectedProjects)
                    {
                        int projectId = Convert.ToInt32(projectid);
                        projecthelper.AddUserToProject(user.Id, projectId);
                    }
                }

                // Work on Role Assignments
                // Only Admin Role can edit Role Assignments
                if (User.IsInRole("Admin"))
                {

                    var userRoles = rolehelper.ListUserRoles(user.Id);

                    //DELETE current Roles)
                    foreach (var roleName in userRoles)
                    {
                        rolehelper.RemoveUserFromRole(user.Id, roleName);
                    }

                    //ADD the whole list of new Roles)
                    if (model.SelectedRoles != null)
                    {
                        foreach (var roleName in model.SelectedRoles)
                        {
                            rolehelper.AddUserToRole(user.Id, roleName);
                        }
                    }
                }

                db.SaveChanges();

            }
            return RedirectToAction("Index");
        }

    }
}