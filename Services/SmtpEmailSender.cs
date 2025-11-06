using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace WebApp.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _config;

        public SmtpEmailSender(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var smtpSection = _config.GetSection("Smtp");

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(smtpSection["FromName"], smtpSection["FromEmail"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(
                smtpSection["Host"], 
                int.Parse(smtpSection["Port"]), 
                SecureSocketOptions.StartTls
            );
            await client.AuthenticateAsync(smtpSection["User"], smtpSection["Pass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
