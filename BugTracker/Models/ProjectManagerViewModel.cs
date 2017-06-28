using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class ProjectManagerViewModel
    {
        public Project Project { get; set; }
        public ApplicationUser User { get; set; }
    }
}