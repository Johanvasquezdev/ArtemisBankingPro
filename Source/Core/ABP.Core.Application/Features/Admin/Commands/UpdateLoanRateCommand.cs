using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    // El bool indica si el prestamo existia (false = controller responde 404)
    public sealed record UpdateLoanRateCommand(int LoanId, decimal NewAnnualInterestRate) : IRequest<bool>;

    public sealed class UpdateLoanRateCommandValidator : AbstractValidator<UpdateLoanRateCommand>
    {
        public UpdateLoanRateCommandValidator()
        {
            RuleFor(x => x.NewAnnualInterestRate).GreaterThanOrEqualTo(0)
                .WithMessage("La tasa de interés anual no puede ser negativa.");
        }
    }

    public sealed class UpdateLoanRateCommandHandler(ILoanService loanService) : IRequestHandler<UpdateLoanRateCommand, bool>
    {
        private readonly ILoanService _loanService = loanService;

        public async Task<bool> Handle(UpdateLoanRateCommand request, CancellationToken cancellationToken)
        {
            var loan = await _loanService.GetByIdAsync(request.LoanId);
            if (loan == null) return false;

            await _loanService.UpdateInterestRateAsync(request.LoanId, request.NewAnnualInterestRate);
            return true;
        }
    }
}
