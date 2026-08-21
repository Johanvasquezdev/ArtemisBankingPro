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
        bool ConfirmHighRisk = false) : IRequest<LoanAssignmentResult>
    {
        public AssignLoanCommand(AssignLoanDto loan, bool ConfirmHighRisk = false)
            : this(loan.ClientId, loan.Amount, loan.AnnualInterestRate, loan.TermInMonths, loan.AdminId, ConfirmHighRisk) { Loan = loan; }
        public AssignLoanDto Loan { get; init; } = new()
        {
            ClientId = ClientId, Amount = Amount, AnnualInterestRate = AnnualRate,
            TermInMonths = MonthsInstallments, AdminId = AdminId
        };
    }

    // Resultado "union": o vino un prestamo creado, o vino una advertencia de riesgo sin confirmar.
    // El controller decide 201 vs 409 mirando IsHighRiskUnconfirmed.
    public sealed record LoanAssignmentResult(
        bool Succeeded,
        bool RequiresRiskConfirmation,
        bool HasActiveLoan,
        bool IsHighRisk,
        decimal AverageDebt,
        decimal CurrentDebt,
        LoanDto? Loan,
        string? Message)
    {
        public bool IsHighRiskUnconfirmed => RequiresRiskConfirmation;
        public string? RiskMessage => Message;
        public string? RiskType => IsHighRisk ? "ProjectedHighRisk" : null;
    }

    public sealed class AssignLoanCommandValidator : AbstractValidator<AssignLoanCommand>
    {
        private static readonly int[] AllowedTerms = [6, 12, 24, 36, 48, 60];

        public AssignLoanCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty().WithMessage("ClientId is required.");
            RuleFor(x => x.AdminId).NotEmpty().WithMessage("AdminId is required.");
            RuleFor(x => x.Loan.TermInMonths).Must(m => AllowedTerms.Contains(m))
                .WithMessage("El plazo seleccionado no es válido.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("El monto del préstamo debe ser mayor que cero.");
            RuleFor(x => x.AnnualRate).GreaterThanOrEqualTo(0).WithMessage("La tasa de interés anual no puede ser negativa.");
        }
    }

    public sealed class AssignLoanCommandHandler(ILoanService loanService) : IRequestHandler<AssignLoanCommand, LoanAssignmentResult>
    {
        private readonly ILoanService _loanService = loanService;

        public async Task<LoanAssignmentResult> Handle(AssignLoanCommand request, CancellationToken cancellationToken)
        {
            if (await _loanService.ClientHasActiveLoanAsync(request.ClientId))
                throw new InvalidOperationException("El cliente ya tiene un préstamo activo.");

            var (isHighRisk, averageDebt, currentDebt) = await _loanService.EvaluateRiskAsync(
                request.ClientId, request.Amount, request.AnnualRate, request.MonthsInstallments);

            if (isHighRisk && !request.ConfirmHighRisk)
            {
                return new LoanAssignmentResult(false, true, false, true, averageDebt, currentDebt, null,
                    "Asignar este préstamo convertirá al cliente en alto riesgo, ya que su deuda superará el promedio del sistema.");
            }

            var created = await _loanService.AssignAsync(request.Loan);
            return new LoanAssignmentResult(true, false, false, isHighRisk, averageDebt, currentDebt, created,
                "Préstamo asignado correctamente.");
        }
    }
}
