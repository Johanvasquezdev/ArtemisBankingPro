using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Client;
using MediatR;

namespace ABP.Core.Application.Features.Client.Queries
{
    public record GetClientHomeQuery(string ClientId) : IRequest<ClientHomeViewModel>;

    public class GetClientHomeQueryHandler(
        IUserReadOnlyService userService,
        ISavingsAccountService accountService,
        ICreditCardService creditCardService,
        ILoanService loanService,
        ILoanInstallmentService installmentService,
        ITransactionService transactionService)
        : IRequestHandler<GetClientHomeQuery, ClientHomeViewModel>
    {
        public async Task<ClientHomeViewModel> Handle(GetClientHomeQuery request, CancellationToken cancellationToken)
        {
            var user = await userService.GetByIdAsync(request.ClientId);

            var accounts = (await accountService.GetByClientIdAsync(request.ClientId))
                .Where(a => a.Status == ABP.Core.Domain.Enums.AccountStatus.Active)
                .ToList();

            var cards = (await creditCardService.GetActiveByClientIdAsync(request.ClientId)).ToList();
            var loans = (await loanService.GetActiveByClientIdAsync(request.ClientId)).ToList();

            var loanItems = new List<ClientLoanItemViewModel>();
            var overdueCount = 0;

            foreach (var loan in loans)
            {
                var installments = (await installmentService.GetByLoanIdAsync(loan.Id)).ToList();
                var pendingInstallments = installments.Where(i => i.Status != ABP.Core.Domain.Enums.InstallmentStatus.Paid).ToList();

                var item = new ClientLoanItemViewModel
                {
                    Id = loan.Id,
                    LoanNumber = loan.LoanNumber,
                    Amount = loan.Amount,
                    PendingAmount = pendingInstallments.Sum(i => i.InstallmentAmount - i.AmountPaid),
                    TotalInstallments = installments.Count,
                    PaidInstallments = installments.Count - pendingInstallments.Count,
                    IsOnTime = !installments.Any(i => i.IsOverdue),
                    Status = loan.Status,
                    NextDueDate = pendingInstallments
                        .OrderBy(i => i.DueDate)
                        .Select(i => (DateTime?)i.DueDate)
                        .FirstOrDefault()
                };

                loanItems.Add(item);
                overdueCount += installments.Count(i => i.IsOverdue);
            }

            var recentTransactions = new List<TransactionDto>();
            foreach (var account in accounts)
            {
                recentTransactions.AddRange(await transactionService.GetByAccountIdAsync(account.Id));
            }

            return new ClientHomeViewModel
            {
                ClientFullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim(),
                TotalBalance = accounts.Sum(a => a.Balance),
                TotalAccounts = accounts.Count,
                TotalCreditCards = cards.Count,
                TotalLoans = loans.Count,
                Accounts = accounts,
                CreditCards = cards,
                Loans = loanItems,
                RecentTransactions = recentTransactions
                    .OrderByDescending(t => t.TransactionDate)
                    .Take(10)
                    .ToList(),
                OverdueInstallmentsCount = overdueCount,
                HasDelinquentLoans = loanItems.Any(l => !l.IsOnTime)
            };
        }
    }
}
