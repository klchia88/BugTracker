using Microsoft.AspNet.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class UsersForRoleViewModel
    {
        public string Role { get; set; }
        public IList<ApplicationUser> Users { get; set; }
    }
}