using System.Reflection;
using ABP.Core.Application.Features.Account.Commands;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Xunit;

namespace ABP.Unit.Tests.Architecture;

public sealed class ArchitectureTests
{
    [Fact]
    public void Domain_must_not_reference_infrastructure()
    {
        var references = typeof(Transaction).Assembly.GetReferencedAssemblies();
        references.Should().NotContain(reference => reference.Name!.Contains("Infraestructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Application_must_not_reference_infrastructure()
    {
        var application = typeof(LogoutCommand).Assembly;
        application.GetReferencedAssemblies()
            .Should().NotContain(reference => reference.Name!.Contains("Infraestructure", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Persistence_must_not_reference_identity_context_project()
    {
        var persistence = Assembly.Load("ABP.Infraestructure.Persistence");
        persistence.GetReferencedAssemblies()
            .Should().NotContain(reference => reference.Name == "ABP.Infraestructure.identity");
    }

    [Fact]
    public void Every_application_command_has_a_validator_except_logout()
    {
        var application = typeof(LogoutCommand).Assembly;
        var commandTypes = application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false }
                && type.Namespace?.Contains("Features", StringComparison.Ordinal) == true
                && type.Namespace?.Contains("Features.Functions", StringComparison.Ordinal) != true
                && type.Name.EndsWith("Command", StringComparison.Ordinal)
                && type != typeof(LogoutCommand))
            .ToArray();

        var validatorTypes = application.GetTypes()
            .Where(type => type.GetInterfaces().Any(@interface =>
                @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>)))
            .SelectMany(type => type.GetInterfaces()
                .Where(@interface => @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IValidator<>))
                .Select(@interface => @interface.GetGenericArguments()[0]))
            .ToHashSet();

        commandTypes.Should().NotBeEmpty();
        commandTypes.Should().OnlyContain(type => validatorTypes.Contains(type),
            "every write operation should be validated before reaching its handler");
    }

    [Fact]
    public void Application_features_follow_command_query_naming()
    {
        var application = typeof(LogoutCommand).Assembly;
        var featureTypes = application.GetTypes()
            .Where(type => type.Namespace?.Contains("Features", StringComparison.Ordinal) == true)
            .Where(type => type.GetInterfaces().Any(@interface =>
                @interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IRequest<>)))
            .ToArray();

        featureTypes.Where(type => type.Name.Contains("Query", StringComparison.Ordinal))
            .Should().NotContain(type => type.Name.Contains("Command", StringComparison.Ordinal));
    }

    [Fact]
    public void Feature_handlers_must_use_narrow_transaction_contracts()
    {
        var application = typeof(LogoutCommand).Assembly;
        var handlers = application.GetTypes()
            .Where(type => type.Namespace?.Contains("Features", StringComparison.Ordinal) == true)
            .Where(type => type.Name.EndsWith("Handler", StringComparison.Ordinal))
            .SelectMany(type => type.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters()))
            .ToArray();

        handlers.Should().NotContain(parameter => parameter.ParameterType == typeof(ITransactionService),
            "feature handlers should depend on the smallest transaction capability they need");
    }
}
