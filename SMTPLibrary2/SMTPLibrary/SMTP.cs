using System;
using System.Collections.Generic;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using System.IO;
using MailKit.Security;

namespace SMTPLibrary2
{
    /// <summary>
    /// Sends email via SMTP. Uses MailKit: https://github.com/jstedfast/MailKit
    /// </summary>
    public class SMTP
    {
        /// <summary>
        /// The mail server address.
        /// </summary>
        public string MailServer { get; set; }

        /// <summary>
        /// The port to use.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// The email address for the sender.
        /// </summary>
        public string SenderEmail { get; set; }

        /// <summary>
        /// The name of the sender.
        /// </summary>
        public string SenderName { get; set; }

        /// <summary>
        /// A semi-colon delimited list of recipients.
        /// </summary>
        public string Recipients { get; set; }

        /// <summary>
        /// A semi-colon delimited list of CC recipients.
        /// </summary>
        public string CCRecipients { get; set; }

        /// <summary>
        /// A semi-colon delimited list of BCC recipients.
        /// </summary>
        public string BCCRecipients { get; set; }

        /// <summary>
        /// A semi-colon delimited list of Reply To addresses.
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// The subject.
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// The user name for the server.
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password for the server.
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// True if the body contains HTML.
        /// </summary>
        public bool UseHtml { get; set; }

        /// <summary>
        /// The SecureSocketOptions setting (see http://www.mimekit.net/docs/html/T_MailKit_Security_SecureSocketOptions.htm)
        /// </summary>
        public int SecurityOptions { get; set; } = 1;

        /// <summary>
        /// An OAuth2 authentication object.
        /// </summary>
        public SaslMechanism OAuthenticate { get; set; }

        /// <summary>
        /// The timeout in seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// The name of a log file to write to.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// A list of attachments.
        /// </summary>
        private List<string> _attachments = new List<string>();

        /// <summary>
        /// The constructor.
        /// </summary>
        public SMTP()
        {
            Timeout = 30;
            ServerPort = 25;
        }

        /// <summary>
        /// Send the message.
        /// </summary>
        /// <returns>
        /// True if it succeeds.
        /// </returns>
        public bool SendMail()
        {
            // Create a mail message.
            MimeMessage message = new MimeMessage();

            // Handle addresses.
            message.From.Add(new MailboxAddress(SenderName, SenderEmail));
            foreach (string address in Recipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.To.Add(new MailboxAddress(address.Trim()));
            }
            foreach (string address in CCRecipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Cc.Add(new MailboxAddress(address.Trim()));
            }
            foreach (string address in BCCRecipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Bcc.Add(new MailboxAddress(address.Trim()));
            }
            if (!String.IsNullOrEmpty(ReplyTo))
            {
                foreach (string address in ReplyTo.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.ReplyTo.Add(new MailboxAddress(address.Trim()));
                }
            }

            // Set up the subject, body, and attachments.
            message.Subject = Subject;
            var builder = new BodyBuilder();
            if (UseHtml)
            {
                builder.HtmlBody = Message;
            }
            else
            {
                builder.TextBody = Message;
            }
            foreach (string path in _attachments)
            {
                builder.Attachments.Add(path);
            }
            message.Body = builder.ToMessageBody();

            // Send the message.
            SmtpClient client;
            if (String.IsNullOrWhiteSpace(LogFile))
            {
                client = new SmtpClient();
            }
            else
            {
                client = new SmtpClient(new ProtocolLogger(LogFile));
            }
            using (client)
            {
                // Accept all SSL certificates (in case the server supports STARTTLS).
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Timeout = Timeout * 1000;
                // See https://unop.uk/sending-email-in-.net-core-with-office-365-and-mailkit/ for Office 365 or 
                // https://unop.uk/advanced-email-sending-with-.net-core-and-mailkit/ for a more advanced guide
                SecureSocketOptions value = (SecureSocketOptions)SecurityOptions;
                client.Connect(MailServer, ServerPort, value);
                if (OAuthenticate != null)
                {
                    client.Authenticate(OAuthenticate);
                }
                else if (!String.IsNullOrWhiteSpace(UserName))
                {
                    // TODO: may have to use System.Text.Encoding.UTF8 as first parameter; see https://github.com/jstedfast/MailKit/issues/686
                    // See https://dotnetcoretutorials.com/2018/03/18/common-errors-sending-email-mailkit/ for common errors
                    client.Authenticate(UserName, Password);
                }
                client.Send(message);
                client.Disconnect(true);
            }
            client.Dispose();
            return true;
        }

        /// <summary>
        /// Add an attachment.
        /// </summary>
        /// <param name="fileName">
        /// The file name of the attachment.
        /// </param>
        public void AddAttachment(string fileName)
        {
            _attachments.Add(fileName);
        }
    }
}
