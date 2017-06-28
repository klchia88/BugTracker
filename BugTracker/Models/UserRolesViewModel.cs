using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class UserRolesViewModel
    {
        public ApplicationUser User { get; set; }
        public string[] SelectedRoles { get; set; }
        public MultiSelectList RolesList { get; set; }
    }
}