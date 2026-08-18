using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    // AdminId lo extrae el Controller del claim "uid" del JWT y lo pasa aqui.
    // El Handler (capa Application) no debe tocar HttpContext/User directamente:
    // eso rompería Onion Architecture, ya que Application no puede depender de detalles
    // de transporte HTTP. Ese es el trabajo del Controller, capa Presentation.
    public sealed record AssignLoanCommand(
        string ClientId,
        decimal Amount,
        decimal AnnualRate,
        int MonthsInstallments,
        string AdminId,
        bool ConfirmHighRisk = false) : IRequest<AssignLoanResult>;

    // Resultado "union": o vino un prestamo creado, o vino una advertencia de riesgo sin confirmar.
    // El controller decide 201 vs 409 mirando IsHighRiskUnconfirmed.
    public sealed record AssignLoanResult
    {
        public LoanDto? Loan { get; init; }
        public bool IsHighRiskUnconfirmed { get; init; }
        public string? RiskMessage { get; init; }
        public string? RiskType { get; init; }
        public decimal CurrentDebt { get; init; }
        public decimal AverageDebt { get; init; }
    }

    public sealed class AssignLoanCommandValidator : AbstractValidator<AssignLoanCommand>
    {
        private static readonly int[] AllowedTerms = [6, 12, 24, 36, 48, 60];

        public AssignLoanCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty().WithMessage("ClientId is required.");
            RuleFor(x => x.MonthsInstallments).Must(m => AllowedTerms.Contains(m))
                .WithMessage("El plazo seleccionado no es válido.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El monto del préstamo debe ser mayor que cero.");
            RuleFor(x => x.AnnualRate).GreaterThanOrEqualTo(0).WithMessage("La tasa de interés anual no puede ser negativa.");
        }
    }

    public sealed class AssignLoanCommandHandler(ILoanService loanService) : IRequestHandler<AssignLoanCommand, AssignLoanResult>
    {
        private readonly ILoanService _loanService = loanService;

        public async Task<AssignLoanResult> Handle(AssignLoanCommand request, CancellationToken cancellationToken)
        {
            if (await _loanService.ClientHasActiveLoanAsync(request.ClientId))
                throw new InvalidOperationException("El cliente ya tiene un préstamo activo.");

            var (isHighRisk, averageDebt, currentDebt) = await _loanService.EvaluateRiskAsync(
                request.ClientId, request.Amount, request.AnnualRate, request.MonthsInstallments);

            if (isHighRisk && !request.ConfirmHighRisk)
            {
                return new AssignLoanResult
                {
                    IsHighRiskUnconfirmed = true,
                    RiskType = currentDebt > averageDebt ? "CurrentHighRisk" : "ProjectedHighRisk",
                    RiskMessage = "Asignar este préstamo convertirá al cliente en alto riesgo, ya que su deuda superará el promedio del sistema.",
                    CurrentDebt = currentDebt,
                    AverageDebt = averageDebt
                };
            }

            var dto = new AssignLoanDto
            {
                ClientId = request.ClientId,
                Amount = request.Amount,
                AnnualInterestRate = request.AnnualRate,
                TermInMonths = request.MonthsInstallments,
                AdminId = request.AdminId
            };

            var created = await _loanService.AssignAsync(dto);
            return new AssignLoanResult { Loan = created };
        }
    }
}
