using System.Net;
using System.Net.Mail;

namespace programowanie_zaawansowane.Services
{
    public class EmailSender
    {
        private readonly string _smtpServer = "smtp.gmail.com";
        private readonly int _smtpPort = 587;
        private readonly string _fromEmail = Environment.GetEnvironmentVariable("SMTP_EMAIL");
        private readonly string _fromPassword = Environment.GetEnvironmentVariable("SMTP_PASSWORD");


        public void SendEmail(string toEmail, string subject, string body)
        {
            var smtpClient = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_fromEmail, _fromPassword),
                EnableSsl = true,
            };

            var message = new MailMessage(_fromEmail, toEmail, subject, body);
            smtpClient.Send(message);
        }
    }
}
