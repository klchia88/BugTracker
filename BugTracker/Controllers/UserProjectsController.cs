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
    public class UserProjectsController : Controller
    {
        private static ApplicationDbContext db = new ApplicationDbContext();
        ProjectAssignHelper helper = new ProjectAssignHelper();

        //
        // GET: /ProjectsForUser/Index
        [Authorize]
        public ActionResult Index()
        {
            List<UserProjectsViewModel> userList = new List<UserProjectsViewModel>();

            foreach (var user in db.Users.OrderBy(p => p.DisplayName).ToList())
            {
                UserProjectsViewModel uservm = new UserProjectsViewModel();
                uservm.User = user;
                uservm.Projects = helper.ListProjectsForUser(user.Id);
                userList.Add(uservm);
            }
            return View(userList);
        }

        //
        // GET: /UsersForProject/Index
        [Authorize]
        public ActionResult UsersForProject()
        {
            List<UsersForProjectViewModel> userList = new List<UsersForProjectViewModel>();

            foreach (var project in db.Projects.OrderBy(p => p.Name).ToList())
            {
                UsersForProjectViewModel uservm = new UsersForProjectViewModel();
                uservm.Project = project;
                uservm.Users = helper.ListUsersOnProject(project.Id);
                userList.Add(uservm);
            }
            return View(userList);
        }

        //
        // GET: /UserProjects/Details
        [Authorize]
        public ActionResult Details(string id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            UserProjectsViewModel uservm = new UserProjectsViewModel();
            ApplicationUser user = db.Users.Find(id);
            uservm.User = user;
            TempData["CurrentUser"] = user;
            uservm.Projects = helper.ListProjectsForUser(id);
            uservm.SelectedProjects = uservm.Projects.Select(p => p.Id).ToArray();
            uservm.ProjectsList = new MultiSelectList(db.Projects.OrderBy(p => p.Name), "Id", "Name", uservm.SelectedProjects);

            if (uservm == null)
            {
                return HttpNotFound();
            }
            return View(uservm);
        }

        // POST: UserProjects/Details
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin,ProjectManager")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Details([Bind(Include = "SelectedProjects,ProjectsList")] UserProjectsViewModel model)
        {

            if (ModelState.IsValid)
            {
                var user = (ApplicationUser)TempData["CurrentUser"];
                var userProjects = helper.ListProjectsForUser(user.Id);

                //DELETE current Projects)
                foreach (var project in userProjects)
                {
                    helper.RemoveUserFromProject(user.Id, project.Id);
                }

                //ADD the whole list of new Projects)
                if (model.SelectedProjects != null)
                {
                    foreach (var projectid in model.SelectedProjects)
                    {
                        int projectId = Convert.ToInt32(projectid);
                        helper.AddUserToProject(user.Id, projectId);
                    }
                }

                db.SaveChanges();

            }
            return RedirectToAction("Index");
        }

    }
}