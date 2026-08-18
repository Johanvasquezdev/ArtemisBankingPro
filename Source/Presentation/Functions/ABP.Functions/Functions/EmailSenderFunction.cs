using ABP.Core.Application.Features.Functions.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions;

public sealed class EmailSenderFunction(IMediator mediator, ILogger<EmailSenderFunction> logger)
{
    [Function(nameof(EmailSenderFunction))]
    public async Task Run([QueueTrigger("email-queue", Connection = "AzureWebJobsStorage")] string message)
    {
        try
        {
            if (!await mediator.Send(new ProcessEmailMessageCommand(message)))
            {
                logger.LogWarning("Email queue message was empty or invalid.");
                return;
            }

            logger.LogInformation("Email queue message processed successfully.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error processing email queue message.");
            throw;
        }
    }
}
