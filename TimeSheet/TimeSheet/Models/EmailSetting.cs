﻿
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

        public static async Task SendEmail(string EmailReceiver, string CC, string EmailType, string id)
        {            
            int ID = Convert.ToInt32(id);
            string link = "https://hr.nantien.edu.au/";
            string body = string.Empty;
            string subject = string.Empty;
            string username = string.Empty;
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            string path = Directory.GetCurrentDirectory();// path for loading email Templates file

            using(TimeSheetDb timesheetDb = new TimeSheetDb())
            {
                switch (EmailType)
                {
                    case "TimesheetApplication":
                        link += "TimesheetApproval/ApprovalDetail/";
                        link += id;
                        subject = "TimesheetApplicaiton";
                        path = path + @"\Templates\TimesheetApplication.txt";
                        using (var sr = new StreamReader(path))
                        {
                            body = sr.ReadToEnd();
                        }
                        TimeRecordForm form = timesheetDb.TimeRecordForms.Find(ID);
                        username = timesheetDb.ADUsers.Find(form.UserID).UserName ?? form.UserID;
                        subject = subject + " From " + username;
                        body = string.Format(body, username, form.Period, form.Year, link);
                        break;
                    case "LeaveApplication":
                        link += "LeaveApproval/ApprovalDetail/";
                        link += id;
                        subject = "LeaveApplicaiton";
                        path = path + @"\Templates\LeaveApplication.txt";
                        using (var sr = new StreamReader(path))
                        {
                            body = sr.ReadToEnd();
                        }
                        LeaveApplication leaveModel = timesheetDb.LeaveApplications.Find(ID);
                        username = leaveModel.UserName ?? leaveModel.UserID;
                        subject = subject + " From " + username;
                        body = string.Format(body, username, leaveModel.StartTime, leaveModel.EndTime, leaveModel.Comment, link);
                        File.AppendAllText(@"C:\Users\Public\TestFolder\test.txt", body + ".");
                        break;
                    case "TimesheetApproval":
                        subject = "TimesheetApproval";
                        path = path + @"\Templates\TimesheetApproval.txt";
                        using (var sr = new StreamReader(path))
                        {
                            body = sr.ReadToEnd();
                        }
                        TimeRecordForm formModel = timesheetDb.TimeRecordForms.Find(ID);
                        ADUser manager = timesheetDb.ADUsers.Find(formModel.ApprovedBy);
                        string comment = string.Empty;
                        if (formModel.Comments != null)
                        {
                            comment = formModel.Comments;
                        }
                        else
                        {
                            comment = "No comments";
                        }
                        body = string.Format(body, formModel.Period, formModel.Year, formModel.status, manager.UserName, comment);
                        break;
                    case "LeaveApproval":
                        subject = "Leave Approval";
                        path = path + @"\Templates\LeaveApproval.txt";
                        using (var sr = new StreamReader(path))
                        {
                            body = sr.ReadToEnd();
                        }
                        LeaveApplication laModel = timesheetDb.LeaveApplications.Find(ID);
                        ADUser leavemanager = timesheetDb.ADUsers.Find(laModel.ApprovedBy);
                        body = string.Format(body, laModel.StartTime, laModel.EndTime, laModel.status, leavemanager.UserName);
                        break;
                }
            }
            
            var message = new MailMessage();
            message.To.Add(new MailAddress(EmailReceiver));
            message.Subject = subject;
            message.Body = body;
            message.IsBodyHtml = true;
            if (CC != "")
            {
                string[] CopyReceiver = CC.Split(';');
                foreach (var cc in CopyReceiver)
                {
                    message.CC.Add(cc);
                }
            }
            try
            {
                Console.WriteLine(message);
                using (var smtp = new SmtpClient())
                {
                    await smtp.SendMailAsync(message);
                }
            }
            catch (Exception ex)
            {
                if (ex.Source != null)
                    Console.WriteLine("IOException source: {0}", ex.Source);
                throw;
            }

        }
    }
}