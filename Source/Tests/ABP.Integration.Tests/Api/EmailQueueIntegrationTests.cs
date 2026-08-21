using ABP.Core.Application.Interfaces.IServices;
using ABP.Functions.Functions;
using ABP.Infraestructure.Shared.EmailServices;
using Azure.Storage.Queues;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using MediatR;
using System;
using ABP.Core.Application.Features.Functions.Commands;

namespace ABP.Integration.Tests.Api;

public class EmailQueueIntegrationTests
{
    [Fact]
    public async Task EmailService_ShouldEnqueueMessage_And_FunctionShouldProcessIt()
    {
        var connectionString = "UseDevelopmentStorage=true";
        var queueName = "email-queue";
        var queueClient = new QueueClient(connectionString, queueName);
        await queueClient.CreateIfNotExistsAsync();
        await queueClient.ClearMessagesAsync();

        var config = new ConfigurationBuilder().AddInMemoryCollection(new[]
        {
            new System.Collections.Generic.KeyValuePair<string, string?>("AzureWebJobsStorage", connectionString)
        }).Build();

        var emailSettings = new Microsoft.Extensions.Options.OptionsWrapper<EmailSettings>(new EmailSettings
        {
            FromEmail = "test@test.com",
            SmtpHost = "localhost",
            SmtpPort = 25,
            SmtpUser = "user",
            SmtpPassword = "pass",
            FromName = "Test"
        });

        var emailService = new EmailService(emailSettings, config);

        await emailService.SendAsync("user@domain.com", "Test", "Hello");

        var msg = await queueClient.ReceiveMessageAsync();
        msg.Value.Should().NotBeNull();
        
        var messageBody = msg.Value.Body.ToString();

        var mediatorMock = new Mock<IMediator>();
        mediatorMock.Setup(m => m.Send(It.IsAny<ProcessEmailMessageCommand>(), default)).ReturnsAsync(true);
        
        var loggerMock = new Mock<ILogger<EmailSenderFunction>>();
        var function = new EmailSenderFunction(mediatorMock.Object, loggerMock.Object);

        await function.Run(messageBody);

        mediatorMock.Verify(m => m.Send(It.IsAny<ProcessEmailMessageCommand>(), default), Times.Once);
    }
}
