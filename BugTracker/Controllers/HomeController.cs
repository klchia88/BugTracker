using BugTracker.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace BugTracker.Controllers
{
    [RequireHttps]
    public class HomeController : Controller
    {
        [Authorize]
        public ActionResult Index()         //@Html.ActionLink("Dashboard", "Index", "Home")
        {
            return View();
        }

        [Authorize]
        public ActionResult Gateway()       //Not in use
        {
            return View();
        }

        [Authorize]
        public ActionResult Statistics()    //@Html.ActionLink("Statistics", "Statistics", "Home")
        {
            return View();
        }

        [Authorize]
        public ActionResult Roles()     //No Longer
        {
            return View();
        }

        [Authorize]
        public ActionResult Icons()     //@Html.ActionLink("Icons", "Icons", "Home")
        {
            return View();
        }

        [Authorize]
        public ActionResult Blank()     //@Html.ActionLink("Blank Page", "Blank", "Home")
        {
            return View();
        }

        [Authorize]
        public ActionResult Error404()      //@Html.ActionLink("Error 404", "Error404", "Home")
        {
            return View();
        }

        [Authorize]
        public ActionResult About()     //Not in use
        {
            return View();
        }

        [Authorize]
        public ActionResult Manual() 
        {
            return View();
        }

        [Authorize]
        public ActionResult QuickStart()
        {
            return View();
        }

        [Authorize]
        public ActionResult Contact()     
        {
            return View();
        }

        /* Return access to C# helper Contact Form  */
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Contact(EmailModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var from = model.FromEmail;
                    var email = new MailMessage(from, ConfigurationManager.AppSettings["emailto"])
                    {
                        Subject = model.Subject,
                        Body = $"<strong>{model.FromName} ({model.FromEmail})</strong><br />left you a message:<br /><br />{model.Body}<br /><br />{model.FromName}<br />",
                        IsBodyHtml = true
                    };
                    var svc = new PersonalEmail();
                    await svc.SendAsync(email);
                    ViewBag.StatusMessage = "Email has been sent";
                    return View();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await Task.FromResult(0);
                }
            }
            return View(model);
        }

    }
}