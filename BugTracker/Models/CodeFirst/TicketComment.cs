using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models.CodeFirst
{
    public class TicketComment
    {
        public int Id { get; set; }
        [AllowHtml]
        public string Comment { get; set; }
        public DateTime Created { get; set; }
        public int TicketId { get; set; }
        public string UserId { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual Ticket Ticket { get; set; }
        //public virtual ICollection<TicketComment> TicketComments { get; set; }

    }
}