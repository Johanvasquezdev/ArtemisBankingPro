using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Domain.Enums;
using MediatR;

namespace ABP.Core.Application.Features.Client.Queries
{
    public record GetTransactionOptionsQuery(string ClientId) : IRequest<TransactionOptionsViewModel>;

    public class GetTransactionOptionsQueryHandler(
        ISavingsAccountService accountService,
        ICreditCardService creditCardService,
        ILoanService loanService,
        IBeneficiaryService beneficiaryService)
        : IRequestHandler<GetTransactionOptionsQuery, TransactionOptionsViewModel>
    {
        public async Task<TransactionOptionsViewModel> Handle(
            GetTransactionOptionsQuery request,
            CancellationToken cancellationToken)
        {
            var accounts = (await accountService.GetByClientIdAsync(request.ClientId))
                .Where(a => a.Status == AccountStatus.Active)
                .ToList();

            var cards = (await creditCardService.GetActiveByClientIdAsync(request.ClientId)).ToList();
            var loans = (await loanService.GetActiveByClientIdAsync(request.ClientId)).ToList();

            var beneficiaries = (await beneficiaryService.GetByOwnerIdAsync(request.ClientId))
                .Select(b => new BeneficiaryListItemViewModel
                {
                    Id = b.Id,
                    AccountNumber = b.AccountNumber,
                    FullName = $"{b.FirstName} {b.LastName}".Trim()
                })
                .ToList();

            return new TransactionOptionsViewModel
            {
                Accounts = accounts,
                CreditCards = cards,
                Loans = loans,
                Beneficiaries = beneficiaries
            };
        }
    }
}
