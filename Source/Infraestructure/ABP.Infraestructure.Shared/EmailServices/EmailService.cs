using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
namespace ABP.Infraestructure.Shared.EmailServices
{
    public class EmailService(IOptions<EmailSettings> settings) : ICorreoServices
    {
        private readonly EmailSettings _settings = settings.Value;

        public async Task SendAsync(string to, string subject, string body)
        {
            var request = new EmailRequest
            {
                To = to,
                Subject = subject,
                Body = body,
                IsHtml = true
            };

            await SendEmailAsync(request);
        }

        public async Task SendEmailAsync(EmailRequest request)
        {
            var email = new MimeMessage();

            email.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
            email.To.Add(MailboxAddress.Parse(request.To));
            email.Subject = request.Subject;

            var builder = new BodyBuilder();

            if (request.IsHtml)
                builder.HtmlBody = request.Body;
            else
                builder.TextBody = request.Body;

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            await smtp.ConnectAsync(
                _settings.SmtpHost,
                _settings.SmtpPort,
                _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls
            );

            await smtp.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}
