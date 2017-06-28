using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models
{
    public class UserPreferencesViewModel
    {
        public string FilterByTickets { get; set; }
        public string FilterByStatus { get; set; }
        //public string MyIndex { get; set; }
        //public bool MyUser { get; set; }
        //public bool MyProjects { get; set; }
        public List<Ticket> Tickets { get; set; }
    }
}