using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ABM.Helpers
{
    public class MailService : IEmailSender
    {
        private readonly IConfiguration _configuration;

        public MailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var smtpConfig = _configuration.GetSection("Smtp");

            try
            {
                using (var smtpClient = new SmtpClient(smtpConfig["Host"]))
                {
                    smtpClient.Port = int.Parse(smtpConfig["Port"]);
                    smtpClient.Credentials = new NetworkCredential(smtpConfig["UserName"], smtpConfig["Password"]);
                    smtpClient.EnableSsl = bool.Parse(smtpConfig["EnableSsl"]);

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(smtpConfig["UserName"]),
                        Subject = subject,
                        Body = htmlMessage,
                        IsBodyHtml = true,
                    };

                    mailMessage.To.Add(email);

                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {

                throw new InvalidOperationException("Error sending email.", ex);
            }
        }

    }
}

