using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetSavingsAccountTransactionsQuery(string AccountNumber, int Page = 1, int PageSize = 20)
        : IRequest<GetSavingsAccountTransactionsResult?>;

    public sealed record GetSavingsAccountTransactionsResult(
        SavingsAccountDto Account, int Page, int PageSize, int TotalRecords, IEnumerable<TransactionDto> Data);

    public sealed class GetSavingsAccountTransactionsQueryValidator : AbstractValidator<GetSavingsAccountTransactionsQuery>
    {
        public GetSavingsAccountTransactionsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20).WithMessage("Parámetros de paginación inválidos.");
        }
    }

    public sealed class GetSavingsAccountTransactionsQueryHandler(ISavingsAccountService accountService)
        : IRequestHandler<GetSavingsAccountTransactionsQuery, GetSavingsAccountTransactionsResult?>
    {
        private readonly ISavingsAccountService _accountService = accountService;

        public async Task<GetSavingsAccountTransactionsResult?> Handle(
            GetSavingsAccountTransactionsQuery request, CancellationToken cancellationToken)
        {
            var account = await _accountService.GetByAccountNumberAsync(request.AccountNumber);
            if (account == null) return null;

            var transactions = await _accountService.GetTransactionsAsync(request.AccountNumber);
            var page = transactions.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize);

            return new GetSavingsAccountTransactionsResult(account, request.Page, request.PageSize, transactions.Count(), page);
        }
    }
}
