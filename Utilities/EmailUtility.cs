
using System.Collections.Generic;
using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;

namespace Lizelaser0310.Utilities
{
    public static class EmailUtility
    {
        public static async Task SendEmail(EmailCredentials credentials, string sender, string subject, Dictionary<string,string> addressee, string messageBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(sender, credentials.Email));
            foreach (var (key, value) in addressee)
            {
                message.To.Add(new MailboxAddress(key, value));
            }
            message.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = messageBody;
            
            message.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            client.Connect(credentials.Host, credentials.Port, credentials.UseSSL);

            // Note: only needed if the SMTP server requires authentication
            client.Authenticate(credentials.Email, credentials.Password);
            await client.SendAsync(message);
            client.Disconnect(true);
        }
    }
}