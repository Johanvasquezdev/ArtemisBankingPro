using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions
{
    /// <summary>
    /// Runs daily. Reviews pending loan installments and marks the ones whose due date
    /// already passed and are not fully paid as overdue (IsOverdue = true), per the
    /// "Control automatico de cuotas atrasadas" requirement in the functional document.
    /// </summary>
    public class LoanLateFeeAndInterestFunction(ILoanInstallmentRepository installmentRepo, ILoanRepository loanRepo,
        ILogger<LoanLateFeeAndInterestFunction> logger)
    {
        private readonly ILoanInstallmentRepository _installmentRepo = installmentRepo;
        private readonly ILoanRepository _loanRepo = loanRepo;
        private readonly ILogger<LoanLateFeeAndInterestFunction> _logger = logger;

        // Runs every day at 00:30 (after the DailyIndicatorFunction, which runs at midnight)
        [Function(nameof(LoanLateFeeAndInterestFunction))]
        public async Task Run([TimerTrigger("0 30 0 * * *")] TimerInfo timer)
        {
            _logger.LogInformation("LoanLateFeeAndInterestFunction started at: {Time}", DateTime.UtcNow);

            int markedOverdue = 0;
            int clearedOverdue = 0;

            try
            {
                // 1. Mark newly overdue installments (due date passed, not fully paid, not yet flagged)
                var overdueInstallments = await _installmentRepo.GetOverdueInstallmentsAsync();

                foreach (var installment in overdueInstallments)
                {
                    if (!installment.IsOverdue)
                    {
                        installment.IsOverdue = true;
                        await _installmentRepo.UpdateAsync(installment);
                        markedOverdue++;
                    }
                }

                // 2. Clear the overdue flag on installments that were paid off since the last run
                //    (the doc requires: "Si una cuota atrasada es pagada posteriormente, el sistema
                //    debe actualizarla para que ya no aparezca como atrasada".)
                var allActiveLoans = await _loanRepo.GetAllAsync();
                foreach (var loan in allActiveLoans.Where(l => l.Status == LoanStatus.Active))
                {
                    var installments = await _installmentRepo.GetByLoanIdAsync(loan.Id);
                    foreach (var installment in installments)
                    {
                        var isFullyPaid = installment.AmountPaid >= installment.InstallmentAmount;
                        if (installment.IsOverdue && isFullyPaid)
                        {
                            installment.IsOverdue = false;
                            await _installmentRepo.UpdateAsync(installment);
                            clearedOverdue++;
                        }
                    }
                }

                _logger.LogInformation( "LoanLateFeeAndInterestFunction finished. Installments marked overdue: {Marked}. Installments cleared: {Cleared}.",
                    markedOverdue, clearedOverdue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while calculating loan overdue status.");
                throw;
            }

            if (timer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next schedule at: {Next}", timer.ScheduleStatus.Next);
            }
        }
    }
}
