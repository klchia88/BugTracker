using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BugTracker.Models;
using BugTracker.Models.CodeFirst;
using Microsoft.AspNet.Identity;
using System.IO;
using System.Web.Routing;
using static BugTracker.Controllers.AccountController;

namespace BugTracker.Controllers
{
    [RequireHttps]
    public class TicketsController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        TicketHistoryHelper historyhelper = new TicketHistoryHelper();
        ProjectAssignHelper projecthelper = new ProjectAssignHelper();

        // GET: Tickets Index & Preferences 
        [Authorize]
        public ActionResult Index()
        {
            //// Phase1 Enhancement: Move UserPreferences TempData to Login, LoginDemo "Account", but hit other problems like, already logged in
            //// Phase1 Enhancement: TempData["UserPreferences"] is read in at Start of Action
            //var userPref = (UserPreferencesViewModel)TempData["UserPreferences"];
            //if (userPref.FilterByTickets == null || userPref.FilterByStatus == null)  //  <= this doesn't work if not instantiated
            //{
            //    userPref.FilterByTickets = "ALL";
            //    userPref.FilterByStatus = "ALL";
            //}
            //// Phase2 Enhancement: use UserPreferences db Model to read and save UserPreferences
            // Current Initial Implementation: First time in TempData["UserPreferences"] is setup 
            UserPreferencesViewModel userPref = new UserPreferencesViewModel();
            TempData["UserPreferences"] = userPref;
            userPref.FilterByTickets = "ALL";
            userPref.FilterByStatus = "ALL";

            // Set things up at start of Action
            var myUserId = User.Identity.GetUserId();
            // Currently we get all Tickets at start of Action
            var tickets = db.Tickets.Include(t => t.AssignedToUser).Include(t => t.OwnerUser).Include(t => t.Project).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType);
            var ticketua = tickets.Where(t => t.TicketStatus.Name != "ARCHIVED").ToList();  // Only get all Unarchived Tickets at start of Action

            var allUnarchived = db.Tickets.Where(t => t.TicketStatus.Name != "ARCHIVED").Count();
            ViewBag.AllUnarchived = allUnarchived;
            var allOpen = db.Tickets.Where(t => t.TicketStatus.Name == "OPEN").Count();
            ViewBag.AllOpen = allOpen;
            var allClosed = db.Tickets.Where(t => t.TicketStatus.Name == "CLOSED").Count();
            ViewBag.AllClosed = allClosed;
            var allArchived = db.Tickets.Where(t => t.TicketStatus.Name == "ARCHIVED").Count();
            ViewBag.AllArchived = allArchived;

            userPref.Tickets = ticketua;
            TempData["UserPreferences"] = userPref;

            ViewBag.FilterByTickets = userPref.FilterByTickets;
            ViewBag.FilterByStatus = userPref.FilterByStatus;

            return View(userPref);
        }

        // POST: Tickets Index & Preferences
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult Index([Bind(Include = "FilterByTickets,FilterByStatus,Tickets")] UserPreferencesViewModel model)
        {
            if (ModelState.IsValid)
            {
                // TempData["UserPreferences"] is read in at Start of Action
                var userPref = (UserPreferencesViewModel)TempData["UserPreferences"];
                var myUserId = User.Identity.GetUserId();
                List<Ticket> ticketvm = new List<Ticket>();  // Start with an empty list

                // Currently we get all Tickets at start of Action and NOT // Taking Tickets from VM that is passed around
                var tickets = db.Tickets.Include(t => t.AssignedToUser).Include(t => t.OwnerUser).Include(t => t.Project).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType);
                var ticketua = tickets.Where(t => t.TicketStatus.Name != "ARCHIVED").ToList();  // Only get all Unarchived Tickets at start of Action

                // Currently NOT dealing with Status Filter from VM that is passed around
                // "model" came into this view and is fixed up for final exit out of this view
                // "ticketvm" is temporary storage to hold tickets while being worked on 

                // if authenticated but not one of the 4 roles, this setting makes stuff he sees only viewable, not editable
                if (!User.IsInRole("Admin") && !User.IsInRole("ProjectManager") && !User.IsInRole("Developer") && !User.IsInRole("Submitter"))
                {
                    model.FilterByTickets = "ALL";
                }

                // Admin role or FilterByTickets == "ALL", return all tickets
                if (User.IsInRole("Admin") || (model.FilterByTickets == "ALL"))
                {
                    model.Tickets = ticketua;  // This is all Unarchived Tickets going into return 
                }
                else
                {

                    // else if FilterByTickets == "MINE", start with empty ticketvm list and add on each Role (of this User's) tickets
                    // Submitter role can only edit tickets they own
                    if ((model.FilterByTickets == "MINE") && (User.IsInRole("Submitter")))
                    {
                        var ticketsub = ticketua.Where(p => p.OwnerUserId.Contains(myUserId));
                        ticketvm = ticketsub.ToList();  // This is start of VM list
                    }

                    // Developer role can only edit tickets they are assigned to, in projects they are assigned to
                    if ((model.FilterByTickets == "MINE") && (User.IsInRole("Developer")))
                    {
                        foreach (Ticket t in ticketua)
                        {
                            if (t.AssignedToUserId == myUserId)
                                ticketvm.Add(t);      // Add each Ticket to VM list
                        }

                        // Ticket.AssignedToUserId could be NULL and that's causing problems with C# methods
                    }

                    // ProjectManager role can only edit tickets in the projects they are in charge of
                    if ((model.FilterByTickets == "MINE") && (User.IsInRole("ProjectManager")))
                    {
                        // Summary logic: tickets which have Projects where ProjectManagerUserId = myUserId 

                        // 1st, get all projects that signed-on user is PM of
                        var pmprojects = db.Projects.Where(u => u.ProjectManagerUserId == myUserId).ToList();

                        //// 2nd, get all tickets which have these Projects
                        //// This is new school way which works but I don't understand
                        //foreach (Project p in pmprojects)
                        //{
                        //    foreach (Ticket t in p.Tickets)       // foreach (Ticket t in p.ticketua) Tried this but doesn't work
                        //    {
                        //        ticketvm.Add(t);  // Add each Ticket to VM list
                        //    }
                        //}

                        // This is my old school way which also works and I understand
                        foreach (Project p in pmprojects)
                        {
                            foreach (Ticket t in ticketua)
                            {
                                if (t.ProjectId == p.Id)
                                {
                                    ticketvm.Add(t);  // Add each Ticket to VM list
                                }
                            }
                        }
                    }

                    // Lastly, make sure to get distinct tickets, i.e. eliminate duplicate tickets pulled by the different queries 

                    // This is new school way which works
                    if (ticketvm.Count() > 0)
                    {
                        var distinctIds = ticketvm.Select(x => x.Id).Distinct();
                        model.Tickets = db.Tickets.Where(t => distinctIds.Contains(t.Id)).ToList();
                    }
                    else
                    {
                        model.Tickets = ticketvm;
                    }

                    //// This is my old school way which also works
                    //List<Ticket> sortTickets = ticketvm.OrderBy(i => i.Id).ToList();
                    //model.Tickets.Clear();
                    //int prevId = 0;
                    //foreach (Ticket t in sortTickets)
                    //{
                    //    if (t.Id != prevId)
                    //    {
                    //        model.Tickets.Add(t);  // Add each Ticket to VM list
                    //        prevId = t.Id;
                    //    }
                    //}

                }   // END of All "if's"

                var allUnarchived = model.Tickets.Where(t => t.TicketStatus.Name != "ARCHIVED").Count();
                ViewBag.AllUnarchived = allUnarchived;
                var allOpen = model.Tickets.Where(t => t.TicketStatus.Name == "OPEN").Count();
                ViewBag.AllOpen = allOpen;
                var allClosed = model.Tickets.Where(t => t.TicketStatus.Name == "CLOSED").Count();
                ViewBag.AllClosed = allClosed;
                var allArchived = db.Tickets.Where(t => t.TicketStatus.Name == "ARCHIVED").Count();
                ViewBag.AllArchived = allArchived;

                // TempData["UserPreferences"] is rewritten at End of Action
                TempData["UserPreferences"] = model;

                ViewBag.FilterByTickets = model.FilterByTickets;
                ViewBag.FilterByStatus = model.FilterByStatus;

                return View(model);
            }
            return View();

        }

        // GET: Tickets/Details/5
        [NoDirectAccess]
        [Authorize]
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // TempData["UserPreferences"] is read in at Start of Action
            var userPref = (UserPreferencesViewModel)TempData["UserPreferences"];

            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            var myUserId = User.Identity.GetUserId();

            bool mySubTicket = false;
            if (User.IsInRole("Submitter") && (ticket.OwnerUserId == myUserId))
            {
                mySubTicket = true;
                ViewBag.MyEditSub = mySubTicket;
            }

            bool myDevTicket = false;
            if (User.IsInRole("Developer") && (ticket.AssignedToUserId == myUserId))
            {
                myDevTicket = true;
                ViewBag.MyEditDev = myDevTicket;
            }

            bool myPMTicket = false;
            if (User.IsInRole("ProjectManager"))
            {
                ProjectAssignHelper projecthelper = new ProjectAssignHelper();
                myPMTicket = projecthelper.IsUserTheProjectManager(myUserId, ticket.ProjectId);
                ViewBag.MyEditSub = myPMTicket;
            }

            ViewBag.MyEditTicket = false;
            if (User.IsInRole("Admin") || myPMTicket == true || myDevTicket == true || mySubTicket == true)
            {
                ViewBag.MyEditTicket = true;
            }

            // TempData["UserPreferences"] is rewritten at End of Action
            TempData["UserPreferences"] = userPref;

            ViewBag.FilterByTickets = userPref.FilterByTickets;
            ViewBag.FilterByStatus = userPref.FilterByStatus;

            return View(ticket);
        }

        // GET: Tickets/Create
        [Authorize(Roles = "Submitter")]
        public ActionResult Create()
        {
            ViewBag.ProjectId = new SelectList(db.Projects.OrderBy(p => p.Name), "Id", "Name");
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name");
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name");
            ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName");
            ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName");

            return View();
        }

        // POST: Tickets/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Submitter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult Create([Bind(Include = "Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,AssignedToUserId,Created,Updated")] Ticket ticket)
        public ActionResult Create([Bind(Include = "Id,Title,Description,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.OwnerUserId = User.Identity.GetUserId();
                var uaUser = db.Users.FirstOrDefault(e => e.Email == "klunassigned@mailinator.com");
                ticket.AssignedToUserId = uaUser.Id;
                ticket.Created = DateTime.Now;
                ticket.Updated = DateTime.Now;
                db.Tickets.Add(ticket);
                db.SaveChanges();

                TicketHistory tickethistory = new TicketHistory();
                tickethistory.TicketId = ticket.Id;
                tickethistory.Property = "All Properties";
                tickethistory.OldValue = "None";
                tickethistory.NewValue = "Created new ticket";
                tickethistory.Changed = DateTime.Now;
                tickethistory.UserId = User.Identity.GetUserId();
                db.Entry(tickethistory).State = EntityState.Modified;
                db.TicketHistories.Add(tickethistory);
                db.SaveChanges();

                // Redirects To "Index" Get Action do not save TempData["UserPreferences"] as TempData is refreshed there
                return RedirectToAction("Index");
            }

            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.OwnerUserId);
            ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.AssignedToUserId);

            return View(ticket);
        }

        // GET: Tickets/Edit/5
        [NoDirectAccess]
        [Authorize(Roles = "Admin,ProjectManager,Developer,Submitter")]
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // TempData["UserPreferences"] is read in at Start of Action
            var userPref = (UserPreferencesViewModel)TempData["UserPreferences"];

            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            var devRoleId = db.Roles.First(r => r.Name == "Developer").Id;
            var projectuserslist = projecthelper.ListUsersOnProject(ticket.ProjectId).Where(p => p.Roles.Any(d => d.RoleId == devRoleId));
            ViewBag.AssignedToUserId = new SelectList(projectuserslist, "Id", "DisplayName", ticket.AssignedToUserId);

            /* SelectList(IEnumerable, String, String, Object)
            Initializes a new instance of the SelectList class by using the specified items for the list, 
            the data value field, the data text field, and a selected value. */

            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.OwnerUserId);

            var myUserId = User.Identity.GetUserId();

            bool mySubTicket = false;
            if (User.IsInRole("Submitter") && (ticket.OwnerUserId == myUserId))
            {
                mySubTicket = true;
                ViewBag.MyEditSub = mySubTicket;
            }

            bool myDevTicket = false;
            if (User.IsInRole("Developer") && (ticket.AssignedToUserId == myUserId))
            {
                myDevTicket = true;
                ViewBag.MyEditSub = myDevTicket;
            }

            bool myPMTicket = false;
            if (User.IsInRole("ProjectManager"))
            {
                ProjectAssignHelper projecthelper = new ProjectAssignHelper();
                myPMTicket = projecthelper.IsUserTheProjectManager(myUserId, ticket.ProjectId);
                ViewBag.MyEditSub = myPMTicket;
            }

            ViewBag.MyEditTicket = false;
            if (User.IsInRole("Admin") || myPMTicket == true || myDevTicket == true || mySubTicket == true)
            {
                ViewBag.MyEditTicket = true;
            }

            // TempData["UserPreferences"] is rewritten at End of Action
            TempData["UserPreferences"] = userPref;

            ViewBag.FilterByTickets = userPref.FilterByTickets;
            ViewBag.FilterByStatus = userPref.FilterByStatus;

            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [NoDirectAccess]
        [Authorize(Roles = "Admin,ProjectManager,Developer,Submitter")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "Id,Title,Description,Created,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,AssignedToUserId,Updated")] Ticket ticket)
        public ActionResult Edit([Bind(Include = "Id,Title,Description,Created,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,AssignedToUserId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                // TempData["UserPreferences"] is read in at Start of Action
                var userPref = (UserPreferencesViewModel)TempData["UserPreferences"];

                ticket.AssignedToUser = db.Users.AsNoTracking().FirstOrDefault(u => u.Id == ticket.AssignedToUserId);

                TicketHistory tickethistory = historyhelper.FindTicketChanges(ticket);
                if (tickethistory.Property != "No changes made")
                {
                    tickethistory.UserId = User.Identity.GetUserId();
                    db.Entry(tickethistory).State = EntityState.Modified;
                    db.TicketHistories.Add(tickethistory);
                    db.SaveChanges();
                }

                ticket.Updated = DateTime.Now;
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();

                // TempData["UserPreferences"] is rewritten at End of Action
                TempData["UserPreferences"] = userPref;

                ViewBag.FilterByTickets = userPref.FilterByTickets;
                ViewBag.FilterByStatus = userPref.FilterByStatus;

                if ((tickethistory.Property == "AssignedTo User") && (tickethistory.NewValue != "Unassigned-To User"))
                {
                    TicketNotification ticketnotif = new TicketNotification();
                    ticketnotif.TicketId = tickethistory.TicketId;
                    ticketnotif.UserId = ticket.AssignedToUserId;
                    ticketnotif.Message = "You have been assigned a ticket on BugTracker.Pro.";
                    ticketnotif.Created = DateTime.Now;
                    db.Entry(ticketnotif).State = EntityState.Modified;
                    db.TicketNotifications.Add(ticketnotif);
                    db.SaveChanges();

                    return RedirectToAction("EmailNotification", "Account", new { sendemail = ticket.AssignedToUser.Email, sendmsg = ticketnotif.Message, ticketId = ticketnotif.TicketId });
                }

                if (tickethistory.Property == "Ticket Status" && tickethistory.NewValue == "ARCHIVED")
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    return RedirectToAction("Details", new { id = ticket.Id });
                }
            }

            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.OwnerUserId);
            ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.AssignedToUserId);

            return View(ticket);
        }

        // GET: Tickets/Delete/5
        [NoDirectAccess]
        [Authorize(Roles = "Admin")]
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }
            return View(ticket);
        }

        // POST: Tickets/Delete/5
        [NoDirectAccess]
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]

        public ActionResult DeleteConfirmed(int id)
        {
            Ticket ticket = db.Tickets.Find(id);
            db.Tickets.Remove(ticket);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        // COMMENTS ACTIONS ***********************************************************************************

        // POST: Tickets/CreateComment
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [NoDirectAccess]
        [Authorize(Roles = "Admin,ProjectManager,Developer,Submitter")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult CreateComment([Bind(Include = "Id,TicketId,Comment,Created")] TicketComment ticketcomment)
        {
            if (ModelState.IsValid)
            {
                // TempData["UserPreferences"] is read in at Start of Action
                var userPref = (UserPreferencesViewModel)TempData["UserPreferences"];

                if (ticketcomment.Comment == null)
                {
                    ViewBag.StatusMessage = "Please enter a valid comment.";
                    ViewBag.CommentErrorMsg = "Please enter a valid comment.";
                    return RedirectToAction("Details", new { id = ticketcomment.TicketId });    
                }

                ticketcomment.Created = DateTime.Now;
                ticketcomment.UserId = User.Identity.GetUserId();
                db.TicketComments.Add(ticketcomment);
                db.SaveChanges();

                TicketNotification ticketnotif = new TicketNotification();
                ticketnotif.TicketId = ticketcomment.TicketId;
                ticketnotif.UserId = db.Tickets.Find(ticketnotif.TicketId).AssignedToUserId;
                var sendtoemail = db.Tickets.Find(ticketnotif.TicketId).AssignedToUser.Email;

                ticketnotif.Message = "A new comment was added to a ticket you are assigned to on BugTracker.Pro.";
                ticketnotif.Created = DateTime.Now;
                db.Entry(ticketnotif).State = EntityState.Modified;
                db.TicketNotifications.Add(ticketnotif);
                db.SaveChanges();

                // TempData["UserPreferences"] is rewritten at End of Action
                TempData["UserPreferences"] = userPref;

                ViewBag.FilterByTickets = userPref.FilterByTickets;
                ViewBag.FilterByStatus = userPref.FilterByStatus;

                return RedirectToAction("EmailNotification", "Account", new { sendemail = sendtoemail, sendmsg = ticketnotif.Message, ticketId = ticketnotif.TicketId });
            }
            ViewBag.StatusMessage = "An error occurred submitting your comment; please try again later.";
            ViewBag.CommentErrorMsg = "An error occurred submitting your comment; please try again later.";
            return RedirectToAction("Details", new { id = ticketcomment.TicketId });    

        }
        // *********************************************************************************** COMMENTS ACTIONS 

        // ATTACHMENTS ACTIONS ***********************************************************************************

        // POST: Tickets/UploadAttachment
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [NoDirectAccess]
        [Authorize(Roles = "Admin,ProjectManager,Developer,Submitter")]
        [HttpPost]
        [ValidateAntiForgeryToken]

        public ActionResult UploadAttachment([Bind(Include = "Id,TicketId,FilePath,FileUrl,Description,Created")] TicketAttachment ticketattachment, HttpPostedFileBase file)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {
                // TempData["UserPreferences"] is read in at Start of Action
                var userPref = (UserPreferencesViewModel)TempData["UserPreferences"];

                if (FileUploadValidator.IsWebFriendlyImage(file))
                {
                    var filename = Path.GetFileName(file.FileName);
                    var customname = string.Format(Guid.NewGuid() + filename);
                    file.SaveAs(Path.Combine(Server.MapPath("~/upload/"), customname));
                    ticketattachment.FileUrl = customname;
                }
                else
                {
                    ViewBag.StatusMessage = "Acceptable file formats are: csv, xls, xlsx,  doc, docx, jpg, png, txt, pdf";
                    ViewBag.FileErrorMsg = "Acceptable file formats are: csv, xls, xlsx,  doc, docx, jpg, png, txt, pdf";
                    ticketattachment.FileUrl = "defaultImg.jpg"; // default image
                    return RedirectToAction("Details", new { id = ticketattachment.TicketId });
                }

                ticketattachment.FilePath = "~/upload/";
                ticketattachment.Created = DateTime.Now;
                ticketattachment.UserId = User.Identity.GetUserId();
                db.TicketAttachments.Add(ticketattachment);
                db.SaveChanges();

                TicketNotification ticketnotif = new TicketNotification();
                ticketnotif.TicketId = ticketattachment.TicketId;
                ticketnotif.UserId = db.Tickets.Find(ticketnotif.TicketId).AssignedToUserId;
                var sendtoemail = db.Tickets.Find(ticketnotif.TicketId).AssignedToUser.Email;

                ticketnotif.Message = "A new attachment was uploaded to a ticket you are assigned to on BugTracker.Pro.";
                ticketnotif.Created = DateTime.Now;
                db.Entry(ticketnotif).State = EntityState.Modified;
                db.TicketNotifications.Add(ticketnotif);
                db.SaveChanges();

                // TempData["UserPreferences"] is rewritten at End of Action
                TempData["UserPreferences"] = userPref;

                ViewBag.FilterByTickets = userPref.FilterByTickets;
                ViewBag.FilterByStatus = userPref.FilterByStatus;

                return RedirectToAction("EmailNotification", "Account", new { sendemail = sendtoemail, sendmsg = ticketnotif.Message, ticketId = ticketnotif.TicketId });
            }
            return RedirectToAction("Details", new { id = ticketattachment.TicketId });
        }
        // *********************************************************************************** ATTACHMENTS ACTIONS 
    }
}
