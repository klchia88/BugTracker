using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Security;

namespace BugTracker.Models
{
    public class UserRolesHelper
    {
        private ApplicationDbContext db;
        private UserManager<ApplicationUser> userManager;
        private RoleManager<IdentityRole> roleManager;

        public UserRolesHelper(ApplicationDbContext context)
        {
            userManager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context));
            roleManager = new RoleManager<IdentityRole>(new RoleStore<IdentityRole>(context));
            db = context;
        }

        public bool IsUserInRole(string userId, string roleName)
        {
            return userManager.IsInRole(userId, roleName);
        }

        public IList<string> ListUserRoles(string userId)
        {
            return userManager.GetRoles(userId);
        }

        public IList<string> UserAssignedRoles(string userId)
        {
            return userManager.GetRoles(userId);
        }

        public IList<IdentityRole> ListSelectedRoles(string userId)
        {
            var roles = userManager.GetRoles(userId);
            var selectedRoles = db.Roles.Where(r => roles.Contains(r.Name)).ToList();
            return selectedRoles;
        }

        public IList<string> UserUnassignedRoles(string userId)
        {
            var roles = userManager.GetRoles(userId);
            var nonRoles = db.Roles.Where(r => !roles.Contains(r.Name)).Select(r => r.Name).ToList();
            return nonRoles;
        }
        //public IList<IdentityRole>  ListNonUserRoles(string userId)
        //{
        //    var roles = userManager.GetRoles(userId);
        //    var nonRoles = db.Roles.Where(r => !roles.Contains(r.Name)).ToList();
        //    return nonRoles;
        //}

        public bool AddUserToRole(string userId, string roleName)
        {
            var result = userManager.AddToRole(userId, roleName);
            return result.Succeeded;
        }

        public bool RemoveUserFromRole(string userId, string roleName)
        {
            var result = userManager.RemoveFromRole(userId, roleName);
            return result.Succeeded;
        }

        public IList<ApplicationUser> UsersInRole(string roleName)
        {
            var userIds = roleManager.FindByName(roleName).Users.Select(r => r.UserId);
            return userManager.Users.Where(u => userIds.Contains(u.Id)).ToList();
        }

        public IList<ApplicationUser> UsersNotInRole(string roleName)
        {
            var userIds = Roles.GetUsersInRole(roleName);
            return userManager.Users.Where(u => !userIds.Contains(u.Id)).ToList();
        }

    }
}