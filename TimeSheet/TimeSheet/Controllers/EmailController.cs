using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using TimeSheet.Models;

namespace TimeSheet.Controllers
{
    public class EmailController : Controller
    {
        // GET: Email
        public ActionResult Index()
        {
            Email model = new Email();
            model.FromEmail = User.Identity.Name;
            model.subject = "Leave Application";
            string defaultMessage;
            using (var sr = new StreamReader(Server.MapPath("\\App_Data\\Templates\\" + "LeaveApplicationMessage.txt")))
            {
                defaultMessage = sr.ReadToEnd();
            }
            model.Message = defaultMessage;
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> Index(Email model)
        {
            if (ModelState.IsValid)
            {
                string body;
                using (var sr = new StreamReader(Server.MapPath("\\App_Data\\Templates\\" + "LeaveApplicationEmail.txt")))
                {
                    body = sr.ReadToEnd();
                }
                var message = new MailMessage();
                message.To.Add(new MailAddress(User.Identity.Name));
                message.From = new MailAddress(model.FromEmail);
                message.Subject = model.subject;
                message.Body = string.Format(body, model.Message);
                message.IsBodyHtml = true;

                using (var smtp = new SmtpClient())
                {
                    var credential = new NetworkCredential
                    {
                        UserName = model.FromEmail,
                        Password = "Y137196506dw"
                    };
                    smtp.Credentials = credential;
                    smtp.Host = "smtp.office365.com";
                    smtp.Port = 587;
                    smtp.EnableSsl = true;
                    await smtp.SendMailAsync(message);
                    return RedirectToAction("Sent");
                }
            }
            return View(model);
        }

        public ActionResult Sent()
        {
            return View();
        }
    }
}