using BugTracker.Models.CodeFirst;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;

namespace BugTracker.Models
{
    public class TicketHistoryHelper
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // TicketHistoryHelper is only for Changes to Tickets Edit, not for New Tickets Created

        public TicketHistory FindTicketChanges(Ticket newticket)
        {
            TicketHistory tickethistory = new TicketHistory();

            var myTicketChanged = false;
            var oldticket = db.Tickets.Find(newticket.Id);
            tickethistory.TicketId = newticket.Id;
            tickethistory.Changed = DateTime.Now;

            if (newticket.Title != oldticket.Title)
            {
                myTicketChanged = true;
                tickethistory.Property = "Title";
                tickethistory.OldValue = oldticket.Title;
                tickethistory.NewValue = newticket.Title;
            }

            if (newticket.Description != oldticket.Description)
            {
                myTicketChanged = true;
                tickethistory.Property = "Description";
                tickethistory.OldValue = oldticket.Description;
                tickethistory.NewValue = newticket.Description;
            }

            if (newticket.ProjectId != oldticket.ProjectId)
            {
                myTicketChanged = true;
                tickethistory.Property = "Project";
                tickethistory.OldValue = oldticket.Project.Name;
                tickethistory.NewValue = newticket.Project.Name;
                //tickethistory.NewValue = newticket.ProjectId.ToString();
            }

            if (newticket.TicketTypeId != oldticket.TicketTypeId)
            {
                myTicketChanged = true;
                tickethistory.Property = "Ticket Type";
                tickethistory.OldValue = oldticket.TicketType.Name;
                //tickethistory.NewValue = newticket.TicketType.Name;
                //tickethistory.NewValue = newticket.TicketTypeId.ToString();
                var ticketType = db.TicketTypes.Find(newticket.TicketTypeId);
                tickethistory.NewValue = ticketType.Name;
            }

            if (newticket.TicketPriorityId != oldticket.TicketPriorityId)
            {
                myTicketChanged = true;
                tickethistory.Property = "Ticket Priority";
                tickethistory.OldValue = oldticket.TicketPriority.Name;
                //tickethistory.NewValue = newticket.TicketPriority.Name;
                //tickethistory.NewValue = newticket.TicketPriorityId.ToString();
                var ticketPriority = db.TicketPriorities.Find(newticket.TicketPriorityId);
                tickethistory.NewValue = ticketPriority.Name;
            }

            if (newticket.TicketStatusId != oldticket.TicketStatusId)
            {
                myTicketChanged = true;
                tickethistory.Property = "Ticket Status";
                tickethistory.OldValue = oldticket.TicketStatus.Name;
                //tickethistory.NewValue = newticket.TicketStatus.Name;
                //tickethistory.NewValue = newticket.TicketStatusId.ToString();
                var ticketStatus = db.TicketStatuses.Find(newticket.TicketStatusId);
                tickethistory.NewValue = ticketStatus.Name;
            }

            if (newticket.OwnerUserId != oldticket.OwnerUserId)
            {
                myTicketChanged = true;
                tickethistory.Property = "Owner User";
                tickethistory.OldValue = oldticket.OwnerUser.DisplayName;
                tickethistory.NewValue = newticket.OwnerUser.DisplayName;
                //tickethistory.NewValue = newticket.OwnerUserId;
            }

            if (newticket.AssignedToUserId != oldticket.AssignedToUserId)
            {
                myTicketChanged = true;
                tickethistory.Property = "AssignedTo User";
                tickethistory.NewValue = newticket.AssignedToUser.DisplayName;
                if (oldticket.AssignedToUserId == null)
                {
                    tickethistory.OldValue = "Unassigned-To User";
                }
                else
                {
                    tickethistory.OldValue = oldticket.AssignedToUser.DisplayName;
                }
            }

            if (myTicketChanged == false)
            {
                // No changes were made - Set tickethistory so 
                tickethistory.Property = "No changes made";
                tickethistory.OldValue = "None";
                tickethistory.NewValue = "None";
            }
            return tickethistory;
        }

        //tickethistory.UserId = User.Identity.GetUserId();
        //db.Entry(tickethistory).State = EntityState.Modified;
        //return tickethistory;
    }

}
