
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;

namespace TimeSheet.Models
{
    public class EmailSetting
    {

        [Required]
        [EmailAddress]
        public string FromEmail { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        [EmailAddress]
        public string Username { get; set; }

        [Required]
        public string SMTPHost { get; set; }

        [Required]
        public int SMTPPort { get; set; }

        public static async Task SendEmail(string EmailReceiver, string CC,string EmailType, string id)
        {
            AdminDb adminDb = new AdminDb();
            TimeSheetDb timesheetDb = new TimeSheetDb();
            int ID = Convert.ToInt32(id);
            string link = "http://localhost:44300/";
            string body = string.Empty;
            string subject = string.Empty;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string path = Directory.GetCurrentDirectory();// path for loading email template file
            switch (EmailType)
            {
                case "TimesheetApplication":
                    link += "TimesheetApproval/ApprovalDetail/";
                    link += id;
                    subject = "TimesheetApplicaiton";
                    path = path + @"\App_Data\Template\TimesheetApplication.txt";
                    using (var sr = new StreamReader(path))
                    {
                        body = sr.ReadToEnd();
                    }
                    TimeRecordForm form = timesheetDb.TimeRecordForms.Find(ID);
                    subject = subject + " From " + form.UserID;
                    body = string.Format(body,form.UserID,form.Period,form.Year,link);
                    break;
                case "LeaveApplication":
                    link += "LeaveApproval/ApprovalDetail/";
                    link += id;
                    subject = "LeaveApplicaiton";
                    path = path + @"\App_Data\Template\LeaveApplication.txt";
                    using (var sr= new StreamReader(path))
                    {
                        body = sr.ReadToEnd();
                    }
                    LeaveApplication leaveModel  = timesheetDb.LeaveApplications.Find(ID);
                    subject = subject + " From " + leaveModel.UserID;
                    body = string.Format(body,leaveModel.UserID,leaveModel.StartTime,leaveModel.EndTime,leaveModel.Comment,link);
                    break;
                case "TimesheetApproval":
                    subject = "TimesheetApproval";
                    path = path + @"\App_Data\Template\TimesheetApproval.txt";
                    using (var sr = new StreamReader(path))
                    {
                        body = sr.ReadToEnd();
                    }
                    TimeRecordForm formModel = timesheetDb.TimeRecordForms.Find(ID);
                    Manager manager = (from m in adminDb.ManagerSetting
                                       where m.ManagerID == formModel.ManagerID
                                       select m).FirstOrDefault();
                    string comment = string.Empty;
                    if (formModel.Comments != null)
                    {
                        comment = formModel.Comments;
                    }
                    else
                    {
                        comment = "No comments";
                    }
                    body = string.Format(body, formModel.Period, formModel.Year, formModel.FormStatus, manager.ManagerName, comment);
                    break;
                case "LeaveApproval":
                    subject = "LeaveApproval";
                    path = path + @"\App_Data\Template\LeaveApproval.txt";
                    using (var sr = new StreamReader(path))
                    {
                        body = sr.ReadToEnd();
                    }
                    LeaveApplication laModel = timesheetDb.LeaveApplications.Find(ID);
                    Manager leavemanager = (from m in adminDb.ManagerSetting
                                       where m.ManagerID == laModel.ManagerID
                                       select m).FirstOrDefault();
                    body = string.Format(body, laModel.StartTime,laModel.EndTime,laModel.status,leavemanager.ManagerName);
                    break;
            }
            var message = new MailMessage();
            message.To.Add(new MailAddress(EmailReceiver));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;
            if (CC != "")
            {
                string[] CopyReceiver = CC.Split(';');
                foreach(var cc in CopyReceiver)
                {
                    message.CC.Add(cc);
                }
            }
            try
            {
                using (var smtp = new SmtpClient())
                {
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}