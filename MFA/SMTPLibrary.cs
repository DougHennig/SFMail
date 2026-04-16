// Created by kobruleht: https://github.com/DougHennig/SFMail/issues/15

using System;
using System.Collections.Generic;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using MailKit.Security;
using System.Security.Authentication;
using Microsoft.Identity.Client;
using System.Threading.Tasks;
using System.Linq;

namespace SMTPLibraryCore
{
    /// <summary>
    /// Sends email via SMTP. Uses MailKit: https://github.com/jstedfast/MailKit
    /// </summary>
    public class SMTP
    {
        /// <summary>
        /// The mail server address.
        /// </summary>
        public string MailServer { get; set; } = string.Empty;

        /// <summary>
        /// The port to use.
        /// </summary>
        public int ServerPort { get; set; }

        /// <summary>
        /// The email address for the sender.
        /// </summary>
        public string SenderEmail { get; set; } = string.Empty;

        /// <summary>
        /// The name of the sender.
        /// </summary>
        public string SenderName { get; set; } = string.Empty;

        /// <summary>
        /// A semi-colon delimited list of recipients.
        /// </summary>
        public string Recipients { get; set; } = string.Empty;

        /// <summary>
        /// A semi-colon delimited list of CC recipients.
        /// </summary>
        public string CCRecipients { get; set; } = string.Empty;

        /// <summary>
        /// A semi-colon delimited list of BCC recipients.
        /// </summary>
        public string BCCRecipients { get; set; } = string.Empty;

        /// <summary>
        /// A semi-colon delimited list of Reply To addresses.
        /// </summary>
        public string ReplyTo { get; set; } = string.Empty;

        /// <summary>
        /// The subject.
        /// </summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// The body of the message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The user name for the server.
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// The password for the server.
        /// </summary>
        public string Password { get; set; } = string.Empty;

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
        public SaslMechanism? OAuthenticate { get; set; }

        /// <summary>
        /// The timeout in seconds.
        /// </summary>
        public int Timeout { get; set; }

        /// <summary>
        /// The name of a log file to write to.
        /// </summary>
        public string LogFile { get; set; } = string.Empty;

        /// <summary>
        /// A list of attachments.
        /// </summary>
        private readonly List<string> _attachments = new List<string>();

        /// <summary>
        /// MFA Client ID. If set, will use interactive login 
        /// with MSAL to acquire an access token for SMTP authentication.
        /// See https://docs.microsoft.com/en-us/exchange/client-developer/legacy-protocols/how-to-authenticate-an-imap-pop-smtp-application-by-using-oauth
        /// for details.
        /// </summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>
        /// MFA Tenant ID. Required if ClientId is set.
        /// Gets or sets the unique identifier of the tenant associated with the current context.
        /// </summary>
        public string TenantId { get; set; } = string.Empty;

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
        public async Task<bool> SendMailAsync()
        {

            if (!string.IsNullOrWhiteSpace(ClientId))
            {
                // 1. Initialize MSAL for interactive login (supports MFA)
                string[] scopes = ["https://outlook.office.com/SMTP.Send", "offline_access"];
                var app = PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
                    .WithRedirectUri("http://localhost")
                    .Build();

                // 2. Acquire Access Token
                AuthenticationResult result;
                try
                {
                    // 1. Try to get token silently from the cache
                    var accounts = await app.GetAccountsAsync();
                    result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault())
                        .ExecuteAsync();
                }
                catch (MsalUiRequiredException)
                {
                    // 2. If silent fails (e.g., first time or expired), trigger interactive login
                    // This will open a browser window for the user to complete MFA
                    result = await app.AcquireTokenInteractive(scopes)
                        .ExecuteAsync();
                }
                string accessToken = result.AccessToken;
                OAuthenticate = new SaslMechanismOAuth2(UserName, accessToken);
            }
            // Create a mail message.
            MimeMessage message = new MimeMessage();

            // Handle addresses.
            message.From.Add(new MailboxAddress(SenderName, SenderEmail));
            foreach (string address in Recipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.To.Add(new MailboxAddress("", address.Trim()));
            }
            foreach (string address in CCRecipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Cc.Add(new MailboxAddress("", address.Trim()));
            }
            foreach (string address in BCCRecipients.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                message.Bcc.Add(new MailboxAddress("", address.Trim()));
            }
            if (!string.IsNullOrEmpty(ReplyTo))
            {
                foreach (string address in ReplyTo.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    message.ReplyTo.Add(new MailboxAddress("", address.Trim()));
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
            MimeEntity attachment;
            foreach (string path in _attachments)
            {
                attachment = builder.Attachments.Add(path);
                foreach (var parameter in attachment.ContentType.Parameters)
                {
                    parameter.EncodingMethod = ParameterEncodingMethod.Rfc2047;
                }

                if (attachment.ContentDisposition != null)
                {
                    foreach (var parameter in attachment.ContentDisposition.Parameters)
                    {
                        parameter.EncodingMethod = ParameterEncodingMethod.Rfc2047;
                    }
                }
            }
            message.Body = builder.ToMessageBody();

            // Send the message.
            SmtpClient client;
            if (string.IsNullOrWhiteSpace(LogFile))
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

                // See https://unop.uk/sending-email-in-.net-core-with-office-365-and-mailkit/ for Office 365 or 
                // https://unop.uk/advanced-email-sending-with-.net-core-and-mailkit/ for a more advanced guide
                SecureSocketOptions value = (SecureSocketOptions)SecurityOptions;
                // See https://github.com/jstedfast/MailKit/blob/master/FAQ.md#ssl-handshake-exception for why setting SslProtocols
                client.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;

                // Connect to the server.
                client.Timeout = Timeout * 1000;
                client.Connect(MailServer, ServerPort, value);

                // Authenticate.
                if (OAuthenticate != null)
                {
                    client.Authenticate(OAuthenticate);
                }
                else if (!string.IsNullOrWhiteSpace(UserName))
                {
                    // TODO: may have to use System.Text.Encoding.UTF8 as first parameter; see https://github.com/jstedfast/MailKit/issues/686
                    // See https://dotnetcoretutorials.com/2018/03/18/common-errors-sending-email-mailkit/ for common errors
                    client.Authenticate(UserName, Password);
                }

                // Send the message.
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
