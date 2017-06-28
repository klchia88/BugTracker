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
using Microsoft.Ajax.Utilities;

namespace BugTracker.Controllers
{
    public class TestingController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();
        TicketHistoryHelper historyhelper = new TicketHistoryHelper();
        //private static ApplicationDbContext db3 = new ApplicationDbContext();
        ProjectAssignHelper projecthelper = new ProjectAssignHelper();

        // GET: Tickets Index
        [Authorize]
        public ActionResult Index()
        {
            var tickets = db.Tickets.Include(t => t.AssignedToUser).Include(t => t.OwnerUser).Include(t => t.Project).Include(t => t.TicketPriority).Include(t => t.TicketStatus).Include(t => t.TicketType);

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var myUserId = User.Identity.GetUserId();
            var ticketlist = db.Tickets.AsQueryable();
            var ticketvm = ticketlist.ToList();

            ViewBag.StatusAll = false;
            ViewBag.StatusOpen = false;
            ViewBag.StatusClosed = false;
            ViewBag.StatusArchived = false;
            ViewBag.TicketsAll = false;
            ViewBag.TicketsMine = false;
            // First time TempData["UserPreferences"] is setup in a sequence
            UserPreferencesViewModel prefvm = new UserPreferencesViewModel();
            TempData["UserPreferences"] = prefvm;
            //prefvm.FilterByStatus = "ALL";
            prefvm.Tickets = ticketvm;
            return View(prefvm);
        }

        // POST: Tickets Preferences
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult Index([Bind(Include = "MyUser,MyProjects,Tickets")] UserPreferencesViewModel model)
        //public ActionResult MyIndex(string myUser)
        //public ActionResult MyIndex()
        public ActionResult Index([Bind(Include = "FilterByTickets,FilterByStatus,Tickets")] UserPreferencesViewModel model)

        {
            if (ModelState.IsValid)
            {
                // TempData["UserPreferences"] is read in at Start of Action
                //var prefvm = (UserPreferencesViewModel)TempData["UserPreferences"];
                ViewBag.MyIndex = "MyIndex";
                //model.MyTickets = model.MyTickets;
                //model.MyStatus = model.MyStatus;
                var myUserId = User.Identity.GetUserId();
                var ticketlist = db.Tickets.AsQueryable();
                var ticketvm = ticketlist.ToList();

                // Admin role can see and edit all tickets
                if (User.IsInRole("Admin"))
                {
                    ticketvm = ticketlist.ToList(); // This is whole VM list
                }
                else
                {
                    // Submitter role can only edit tickets they own
                    if (User.IsInRole("Submitter"))
                    {
                        var ticketsub = ticketlist.Where(p => p.OwnerUserId.Contains(myUserId));
                        ticketvm = ticketsub.ToList();  // This is start of VM list
                    }

                    // Developer role can only edit tickets they are assigned to, in projects they are assigned to
                    if (User.IsInRole("Developer"))
                    {
                        var ticketdev = ticketlist.Where(p => p.AssignedToUserId.Contains(myUserId));
                        foreach (Ticket t in ticketdev)
                        {
                            ticketvm.Add(t);      // Add each Ticket to VM list
                        }
                    }

                    // ProjectManager role can only edit tickets in the projects they are in charge of
                    if (User.IsInRole("ProjectManager"))
                    {
                        // Summary logic: tickets which have Projects where ProjectManagerUserId = myUserId 

                        // 1st, get all projects that signed-on user is PM of
                        var pmprojects = db.Projects.Where(u => u.ProjectManagerUserId == myUserId).ToList();
                        //// 2nd, get all tickets which have these Projects
                        //// This code doesn't work => ticketlist = ticketlist.Where(p => p.Project == pmprojects); <= cos pmprojects is a list

                        //// This is Mark's way which works
                        //foreach (Project p in pmprojects)
                        //{
                        //    foreach(Ticket t in p.Tickets)
                        //    {
                        //        ticketvm.Add(t);  // Add each Ticket to VM list
                        //    }
                        //}

                        // This is my old school way which also works
                        foreach (Project p in pmprojects)
                        {
                            foreach (Ticket t in ticketlist)
                            {
                                if (t.ProjectId == p.Id)
                                {
                                    ticketvm.Add(t);  // Add each Ticket to VM list
                                }
                            }
                        }

                    }

                    // Lastly, make sure to get distinct tickets, i.e. eliminate duplicate tickets pulled by the different queries 

                    // Here's the start of one way I don't know how to finish.
                    var distinctIds = ticketvm.Select(x => x.Id).Distinct();
                    model.Tickets = db.Tickets.Where(t => distinctIds.Contains(t.Id)).ToList();

                    // Here's another way I don't know how to finish.
                    //List<Ticket> distinctTickets = ticketvm.OrderBy(i => i.Id).Distinct<Ticket>.ToList();

                    // Here's the old school way I know how to finish.
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
                }

                // TempData["UserPreferences"] is rewritten at End of Action
                TempData["UserPreferences"] = model;

                return View(model);
            }
            return View();

        }

        // GET: Tickets/Details/5
        public ActionResult Details(int? id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            // TempData["UserPreferences"] is read in at Start of Action
            var prefvm = (UserPreferencesViewModel)TempData["UserPreferences"];
            //ViewBag.MyIndex = prefvm.MyIndex;

            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            // TempData["UserPreferences"] is rewritten at End of Action
            TempData["UserPreferences"] = prefvm;
            return View(ticket);
        }

        // GET: Tickets/Create
        public ActionResult Create()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

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
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                ticket.OwnerUserId = User.Identity.GetUserId();
                ticket.Created = DateTime.Now;
                ticket.Updated = DateTime.Now;
                db.Tickets.Add(ticket);
                //db.SaveChanges();

                TicketHistory tickethistory = new TicketHistory();
                tickethistory.TicketId = ticket.Id;
                tickethistory.Property = "All Properties";
                tickethistory.OldValue = "None";
                //tickethistory.NewValue = newticket.ToString();
                tickethistory.NewValue = "Created new ticket";
                tickethistory.Changed = DateTime.Now;
                tickethistory.UserId = User.Identity.GetUserId();
                db.Entry(tickethistory).State = EntityState.Modified;
                db.TicketHistories.Add(tickethistory);
                db.SaveChanges();

                //TicketHistory tickethistory = new TicketHistory();
                //tickethistory.TicketId = ticket.Id;
                //tickethistory.Property = "All";
                //tickethistory.OldValue = "";
                //tickethistory.NewValue = ticket.ToString();
                //tickethistory.Changed = "Created new ticket";
                //tickethistory.UserId = User.Identity.GetUserId();
                //db.TicketHistories.Add(tickethistory);

                //TicketHistory tickethistory = historyhelper.FindTicketChanges(ticket);
                //tickethistory.UserId = User.Identity.GetUserId();
                //db.TicketHistories.Add(tickethistory);
                //db.SaveChanges();

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
        public ActionResult Edit(int? id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Ticket ticket = db.Tickets.Find(id);
            if (ticket == null)
            {
                return HttpNotFound();
            }

            // Mark's code:
            var devRoleId = db.Roles.First(r => r.Name == "Developer").Id;
            var projectuserslist = projecthelper.ListUsersOnProject(ticket.ProjectId).Where(p => p.Roles.Any(d => d.RoleId == devRoleId));
            ViewBag.AssignedToUserId = new SelectList(projectuserslist, "Id", "DisplayName", ticket.AssignedToUserId);

            // My code:
            //var devuserslist = rolehelper.UsersInRole("Developer").ToList();
            //var projectuserslist = projecthelper.ListUsersOnProject(ticket.ProjectId).ToList();
            //var userslist = devuserslist.Intersect(projectuserslist);
            //ViewBag.AssignedToUserId = new SelectList(userslist, "Id", "DisplayName", ticket.AssignedToUserId);
            //var userlist = List<db.Users.IsInRole("Developer") && db.Projects.Where(t => t.Projects.AssignedToUserId == userId)>;
            //ViewBag.AssignedToUserId = new SelectList(userlist, "Id", "DisplayName", ticket.AssignedToUserId);
            //ticketlist = ticketlist.Where(p => p.AssignedToUserId.Contains(myUserId));
            //ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName", User.IsInRole("Developer"));

            /* SelectList(IEnumerable, String, String, Object)
            Initializes a new instance of the SelectList class by using the specified items for the list, 
            the data value field, the data text field, and a selected value. */

            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.OwnerUserId);
            /* ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.AssignedToUserId); */   //Orig code

            return View(ticket);
        }

        // POST: Tickets/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin,ProjectManager,Developer")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public ActionResult Edit([Bind(Include = "Id,Title,Description,Created,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,AssignedToUserId,Updated")] Ticket ticket)
        public ActionResult Edit([Bind(Include = "Id,Title,Description,Created,ProjectId,TicketTypeId,TicketPriorityId,TicketStatusId,OwnerUserId,AssignedToUserId")] Ticket ticket)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                //TicketHistory tickethistory = new TicketHistory();
                //tickethistory.TicketId = ticket.Id;
                //tickethistory.Property = "All";
                //tickethistory.OldValue = "";
                //tickethistory.NewValue = ticket.ToString();
                //tickethistory.Changed = "Created new ticket";
                //tickethistory.UserId = User.Identity.GetUserId();
                //db.Entry(ticket).State = EntityState.Modified;
                //db.TicketHistories.Add(tickethistory);

                TicketHistory tickethistory = historyhelper.FindTicketChanges(ticket);
                tickethistory.UserId = User.Identity.GetUserId();
                db.Entry(tickethistory).State = EntityState.Modified;
                db.TicketHistories.Add(tickethistory);

                //ticket.Created = DateTime.Now;
                ticket.Updated = DateTime.Now;
                db.Entry(ticket).State = EntityState.Modified;
                db.SaveChanges();

                return RedirectToAction("Index");
            }

            ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name", ticket.ProjectId);
            ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name", ticket.TicketPriorityId);
            ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name", ticket.TicketStatusId);
            ViewBag.TicketTypeId = new SelectList(db.TicketTypes, "Id", "Name", ticket.TicketTypeId);
            ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.OwnerUserId);
            ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName", ticket.AssignedToUserId);
            //ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName", User.IsInRole("Developer"));

            return View(ticket);
        }

        // GET: Tickets/Delete/5
        public ActionResult Delete(int? id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

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
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

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
        // GET: Tickets/CreateComment
        public ActionResult CreateComment(int? id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            TicketComment ticketcomment = new TicketComment();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName");
            //ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName");
            //ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");
            //ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
            //ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name");
            ticketcomment.TicketId = Convert.ToInt32(id);

            return View(ticketcomment);
        }

        // POST: Tickets/CreateComment
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateComment([Bind(Include = "Id,Comment,Created,TicketId")] TicketComment ticketcomment)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                //ticketcomment.TicketId = ViewBag.TicketId;
                ticketcomment.Created = DateTime.Now;
                ticketcomment.UserId = User.Identity.GetUserId();
                db.TicketComments.Add(ticketcomment);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        // *********************************************************************************** COMMENTS ACTIONS 

        // ATTACHMENTS ACTIONS ***********************************************************************************
        // GET: Tickets/UploadAttachment
        public ActionResult UploadAttachment(int? id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            TicketAttachment ticketattachment = new TicketAttachment();
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //ViewBag.AssignedToUserId = new SelectList(db.Users, "Id", "DisplayName");
            //ViewBag.OwnerUserId = new SelectList(db.Users, "Id", "DisplayName");
            //ViewBag.ProjectId = new SelectList(db.Projects, "Id", "Name");
            //ViewBag.TicketPriorityId = new SelectList(db.TicketPriorities, "Id", "Name");
            //ViewBag.TicketStatusId = new SelectList(db.TicketStatuses, "Id", "Name");
            ticketattachment.TicketId = Convert.ToInt32(id);

            return View(ticketattachment);
        }

        // POST: Tickets/UploadAttachment
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UploadAttachment([Bind(Include = "Id,TicketId,FilePath,FileUrl,Description,Created")] TicketAttachment ticketattachment, HttpPostedFileBase image)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Account");
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors);
            if (ModelState.IsValid)
            {
                if (ImageUploadValidator.IsWebFriendlyImage(image))
                {
                    var filename = Path.GetFileName(image.FileName);
                    var customname = string.Format(Guid.NewGuid() + filename);
                    image.SaveAs(Path.Combine(Server.MapPath("~/upload/"), customname));
                    ticketattachment.FileUrl = customname;
                }
                else
                {
                    ViewBag.StatusMessage = "Please select proper image";
                    ticketattachment.FileUrl = "defaultImg.jpg"; // default image
                    return View(ticketattachment);
                }

                ticketattachment.FilePath = "~/upload/";
                ticketattachment.Created = DateTime.Now;
                ticketattachment.UserId = User.Identity.GetUserId();
                db.TicketAttachments.Add(ticketattachment);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        // *********************************************************************************** ATTACHMENTS ACTIONS 
    }
}
