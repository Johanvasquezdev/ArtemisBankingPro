using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net;
using System.Text.RegularExpressions;
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

            if (request.IsHtml)
                builder.TextBody = request.TextBody ?? ConvertHtmlToText(request.Body);

            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();

            smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

            var socketOptions = _settings.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : _settings.UseSsl
                    ? SecureSocketOptions.StartTls
                    : SecureSocketOptions.None;

            await smtp.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, socketOptions);

            await smtp.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        private static string ConvertHtmlToText(string html)
        {
            var withLinks = Regex.Replace(
                html,
                "<a[^>]+href=[\\\"']([^\\\"']+)[\\\"'][^>]*>(.*?)</a>",
                "$2 ($1)",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var withLineBreaks = Regex.Replace(
                withLinks,
                "<(br|/p|/div|/h[1-6]|/li|/tr)[^>]*>",
                "\\n",
                RegexOptions.IgnoreCase);
            var withoutTags = Regex.Replace(withLineBreaks, "<[^>]+>", string.Empty);
            var decoded = WebUtility.HtmlDecode(withoutTags);

            return Regex.Replace(decoded, "[ \\t]+\\n", "\\n").Trim();
        }
    }
}
