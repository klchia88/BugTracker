using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class UserAssignmentsViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Project> Projects { get; set; }
        public int[] SelectedProjects { get; set; }
        public MultiSelectList ProjectsList { get; set; }
        public string[] SelectedRoles { get; set; }
        public MultiSelectList RolesList { get; set; }
    }
}