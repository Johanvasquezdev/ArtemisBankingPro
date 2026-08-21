using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Commerce.Commands;

public sealed record ProcessCommercePaymentCommand(int CommerceId, ProcessPaymentDto Payment)
    : IRequest<PaymentResultDto>;

public sealed class ProcessCommercePaymentCommandValidator
    : AbstractValidator<ProcessCommercePaymentCommand>
{
    public ProcessCommercePaymentCommandValidator()
    {
        RuleFor(x => x.CommerceId).GreaterThan(0);
        RuleFor(x => x.Payment.CardNumber).NotEmpty();
        RuleFor(x => x.Payment.MonthExpirationCard).NotEmpty();
        RuleFor(x => x.Payment.YearExpirationCard).NotEmpty();
        RuleFor(x => x.Payment.CVC).NotEmpty();
        RuleFor(x => x.Payment.TransactionAmount).GreaterThan(0);
        RuleFor(x => x.Payment.IdempotencyKey).NotEmpty().MaximumLength(200);
    }
}

public sealed class ProcessCommercePaymentCommandHandler(IPaymentProcessorService payments)
    : IRequestHandler<ProcessCommercePaymentCommand, PaymentResultDto>
{
    public Task<PaymentResultDto> Handle(ProcessCommercePaymentCommand request, CancellationToken cancellationToken) =>
        payments.ProcessPaymentAsync(request.CommerceId, request.Payment);
}
