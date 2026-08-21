using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Exceptions;
using MediatR;

namespace ABP.Core.Application.Features.Client.Queries
{
    public record GetAccountDetailQuery(
        string ClientId,
        string AccountNumber,
        DateTime? DateFrom = null,
        DateTime? DateTo = null) : IRequest<AccountDetailViewModel>;

    public class GetAccountDetailQueryHandler(
        ISavingsAccountService accountService,
        ITransactionQueryService transactionService,
        IUserReadOnlyService userService)
        : IRequestHandler<GetAccountDetailQuery, AccountDetailViewModel>
    {
        public async Task<AccountDetailViewModel> Handle(GetAccountDetailQuery request, CancellationToken cancellationToken)
        {
            var account = await accountService.GetByAccountNumberAsync(request.AccountNumber)
                ?? throw new InvalidAccountException();

            if (account.UserId != request.ClientId)
                throw new InvalidAccountException();

            var transactions = (await transactionService.GetByAccountIdAsync(account.Id)).ToList();

            if (request.DateFrom.HasValue)
                transactions = transactions.Where(t => t.TransactionDate.Date >= request.DateFrom.Value.Date).ToList();

            if (request.DateTo.HasValue)
                transactions = transactions.Where(t => t.TransactionDate.Date <= request.DateTo.Value.Date).ToList();

            var user = await userService.GetByIdAsync(account.UserId);

            return new AccountDetailViewModel
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                Type = account.Type,
                Status = account.Status,
                OwnerFullName = user is null ? string.Empty : $"{user.FirstName} {user.LastName}".Trim(),
                DateFrom = request.DateFrom,
                DateTo = request.DateTo,
                Transactions = transactions
                    .OrderByDescending(t => t.TransactionDate)
                    .ToList()
            };
        }
    }
}
