using System.Text.Json;
using ABP.Core.Application.Interfaces.IServices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions
{
    public class EmailSenderFunction
    {
        private readonly IEmailServices _emailService;
        private readonly ILogger<EmailSenderFunction> _logger;

        public EmailSenderFunction(IEmailServices emailService, ILogger<EmailSenderFunction> logger)
        {
            _emailService = emailService;
            _logger = logger;
        }

        [Function(nameof(EmailSenderFunction))]
        public async Task Run([QueueTrigger("email-queue", Connection = "AzureWebJobsStorage")] string message)
        {
            _logger.LogInformation($"C# Queue trigger function processed: {message}");

            try
            {
                var emailDto = JsonSerializer.Deserialize<EmailDto>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (emailDto != null && !string.IsNullOrEmpty(emailDto.To))
                {
                    await _emailService.SendAsync(emailDto.To, emailDto.Subject, emailDto.Body);
                    _logger.LogInformation($"Email sent successfully to {emailDto.To}");
                }
                else
                {
                    _logger.LogWarning("Email queue message was empty or invalid.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue message.");
                throw;
            }
        }
    }

    public class EmailDto
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
