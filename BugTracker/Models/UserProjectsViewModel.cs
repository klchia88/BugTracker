using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class UserProjectsViewModel
    {
        public ApplicationUser User { get; set; }
        public List<Project> Projects { get; set; }
        public int[] SelectedProjects { get; set; }
        public MultiSelectList ProjectsList { get; set; }
        //public IList<string> Projects { get; set; }
    }
}