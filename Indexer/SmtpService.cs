using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;

namespace SendIndexerToKindle.Helper
{
    public class SmtpService
    {
        private string SenderMail { get; set; }
        private string SenderPass { get; set; }
        private SmtpClient Client { get; set; }
        private bool IsSending { get; set; }


        public SmtpService()
        {
            Client = new SmtpClient
            {
                Port = Convert.ToInt32("587"),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                EnableSsl = true,
                Host = "smtp.gmail.com"
            };
            SenderMail = "tughanbelbek@gmail.com";
            SenderPass = "pzxbwcjbyjruppjm";
            Client.Credentials = new NetworkCredential(SenderMail, SenderPass);
        }


        public bool SendMail(string header, string body, string contactToSend, string attachmentPath)
        {
            IsSending = true;
            bool result = false;
            try
            {
                result = MailJob(header, body, contactToSend, attachmentPath);
                IsSending = false;
            }
            catch (Exception)
            {
                return false;
            }
            return result;
        }

        public bool SendMail(string header, string body, List<string> contactsToSend, string attachmentPath)
        {
            IsSending = true;
            bool result = false;
            try
            {
                contactsToSend.ForEach(x => { result = MailJob(header, body, x); });
                IsSending = false;
            }
            catch (Exception)
            {
                return false;
            }
            return result;
        }

        private bool MailJob(string header, string body, string x)
        {
            bool result;
            MailMessage mail = new MailMessage(SenderMail, x);
            mail.Subject = header;
            mail.Body = body;
            mail.IsBodyHtml = true;
            Client.Send(mail);

            result = true;
            return result;
        }

        private bool MailJob(string header, string body, string x, string attachmentPath)
        {
            bool result;
            MailMessage mail = new MailMessage(SenderMail, x);
            mail.Subject = header;
            mail.Body = body;
            mail.IsBodyHtml = true;
            mail.Attachments.Add(new Attachment(attachmentPath));
            Client.Send(mail);

            result = true;
            return result;
        }
    }
}