using BugTracker.Models.CodeFirst;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace BugTracker.Models
{
    public class ProjectAssignHelper    
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        public bool IsUserOnProject(string userId, int projectId)
        {
            var project = db.Projects.Find(projectId);
            var flag = project.Users.Any(u => u.Id == userId);
            return flag;
        }

        public bool IsUserTheProjectManager(string userId, int projectId)
        {
            var flag = false;
            var project = db.Projects.Find(projectId);
            if (project.ProjectManagerUserId == userId)
            { flag = true; }
            return flag;
        }

        public void AddUserToProject(string userId, int projectId)
        {
            ApplicationUser user = db.Users.Find(userId);
            Project project = db.Projects.Find(projectId);
            project.Users.Add(user);
            db.SaveChanges();
        }

        public void RemoveUserFromProject(string userId, int projectId)
        {
            ApplicationUser user = db.Users.Find(userId);
            Project project = db.Projects.Find(projectId);
            project.Users.Remove(user);
            db.SaveChanges();
        }

        public List<Project> ListProjectsForUser(string userId)
        {
            ApplicationUser user = db.Users.Find(userId);
            return user.Projects.OrderBy(o => o.Name).ToList();
        }

        public List<ApplicationUser> ListUsersOnProject(int projectId)
        {
            Project project = db.Projects.Find(projectId);
            return project.Users.ToList();
        }

        public List<ApplicationUser> ListUsersNotOnProject(int projectId)
        {
            Project project = db.Projects.Find(projectId);
            var usersOnProject = project.Users;
            return db.Users.Where(u => !usersOnProject.Contains(u)).ToList();
        }

    }
}