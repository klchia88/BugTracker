using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Models.CodeFirst
{
    public class TicketHistory
    {
        public int Id { get; set; }
        public int TicketId { get; set; }
        public string Property { get; set; }
        [AllowHtml]
        public string OldValue { get; set; }
        [AllowHtml]
        public string NewValue { get; set; }
        public DateTime Changed { get; set; }
        public string UserId { get; set; }

        public virtual ApplicationUser User { get; set; }
        public virtual Ticket Ticket { get; set; }
        //public virtual ICollection<TicketHistory> TicketHistories { get; set; }
    }
}