using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Text.RegularExpressions;
namespace ABP.Infraestructure.Shared.EmailServices
{
    public class EmailService(IOptions<EmailSettings> settings, IConfiguration configuration) : ICorreoServices
    {
        private readonly EmailSettings _settings = settings.Value;
        private readonly IConfiguration _configuration = configuration;

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

        private string WrapWithTemplate(string subject, string body)
        {
            if (body.TrimStart().StartsWith("<!doctype html", StringComparison.OrdinalIgnoreCase))
                return body;

            return $"""
<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>{subject}</title>
</head>
<body style="margin:0; padding:0; background:#f5f3ee; color:#141414; font-family:Georgia,'Times New Roman',serif;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f5f3ee; padding:32px 12px;">
    <tr>
      <td align="center">
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="max-width:620px; background:#ffffff; border:1px solid #e6dfd2; border-radius:16px; overflow:hidden;">
          <tr>
            <td style="padding:30px 38px; background:#151515;">
              <div style="font-family:Georgia,'Times New Roman',serif; font-size:25px; line-height:1.1; color:#ffffff;">Artemis <span style="color:#c5a059; font-style:italic;">Banking</span></div>
              <div style="margin-top:8px; color:#dfc48c; font-size:11px; letter-spacing:3px;">PRIVATE WEALTH</div>
            </td>
          </tr>
          <tr>
            <td style="padding:42px 38px 36px;">
              <h1 style="margin:0 0 20px; font-family:Georgia,'Times New Roman',serif; font-size:28px; line-height:1.2; font-weight:normal; color:#141414;">{subject}</h1>
              <div style="margin:0; color:#5f625f; font-size:16px; line-height:1.65;">
                {body}
              </div>
            </td>
          </tr>
          <tr>
            <td style="padding:22px 38px; border-top:1px solid #eeeae2; background:#faf9f6; color:#88847b; font-size:12px; line-height:1.6;">Artemis Banking Pro · Private Wealth<br>Este mensaje fue enviado automáticamente; por favor, no respondas a este correo.</td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";
        }

        public async Task SendEmailAsync(EmailRequest request)
        {
            if (request.IsHtml)
            {
                request.Body = WrapWithTemplate(request.Subject, request.Body);
            }

            var connectionString = _configuration.GetConnectionString("AzureWebJobsStorage") ?? Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            var isFunction = Environment.GetEnvironmentVariable("FUNCTIONS_WORKER_RUNTIME") != null;

            if (!string.IsNullOrEmpty(connectionString) && !isFunction)
            {
                var queueClient = new Azure.Storage.Queues.QueueClient(connectionString, "email-queue");
                await queueClient.CreateIfNotExistsAsync();

                var message = new 
                { 
                    To = request.To, 
                    Subject = request.Subject, 
                    Body = request.Body 
                };
                var json = System.Text.Json.JsonSerializer.Serialize(message);
                var bytes = System.Text.Encoding.UTF8.GetBytes(json);
                var base64 = Convert.ToBase64String(bytes);

                await queueClient.SendMessageAsync(base64);
                return;
            }

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
            smtp.CheckCertificateRevocation = _settings.CheckCertificateRevocation;

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
