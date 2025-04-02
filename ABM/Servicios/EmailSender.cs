using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ABM.Servicios;

namespace ABM.Servicios
{
	public class EmailSender : IEmailSender
	{
		private readonly IConfiguration _configuration;

		public EmailSender(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public async Task SendEmailAsync(string email, string subject, string message)
		{
			var smtpServer = _configuration["EmailSettings:SmtpServer"];
			var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]);
			var senderEmail = _configuration["EmailSettings:SenderEmail"];
			var senderPassword = _configuration["EmailSettings:SenderPassword"];

			using (var client = new SmtpClient(smtpServer, smtpPort))
			{
				client.Credentials = new NetworkCredential(senderEmail, senderPassword);
				client.EnableSsl = true;

				var mailMessage = new MailMessage
				{
					From = new MailAddress(senderEmail),
					Subject = subject,
					Body = message,
					IsBodyHtml = true
				};

				mailMessage.To.Add(email);

				await client.SendMailAsync(mailMessage);
			}
		}
	}
}