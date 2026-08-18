using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.LoanInstallment;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries;

public sealed record GetAdminUsersQuery(int Page, int PageSize, UserRole? Role)
    : IRequest<PaginatedResult<UserDto>>;

public sealed class GetAdminUsersQueryHandler(IUserReadOnlyService users)
    : IRequestHandler<GetAdminUsersQuery, PaginatedResult<UserDto>>
{
    public Task<PaginatedResult<UserDto>> Handle(GetAdminUsersQuery request, CancellationToken cancellationToken) =>
        users.GetAllAsync(request.Page, request.PageSize, request.Role);
}

public sealed record GetAdminCommerceUsersQuery(int Page, int PageSize)
    : IRequest<PaginatedResult<UserDto>>;

public sealed class GetAdminCommerceUsersQueryHandler(IUserReadOnlyService users)
    : IRequestHandler<GetAdminCommerceUsersQuery, PaginatedResult<UserDto>>
{
    public Task<PaginatedResult<UserDto>> Handle(GetAdminCommerceUsersQuery request, CancellationToken cancellationToken) =>
        users.GetCommerceUsersAsync(request.Page, request.PageSize);
}

public sealed record GetAdminUserQuery(string UserId) : IRequest<UserDto?>;

public sealed class GetAdminUserQueryHandler(IUserReadOnlyService users)
    : IRequestHandler<GetAdminUserQuery, UserDto?>
{
    public async Task<UserDto?> Handle(GetAdminUserQuery request, CancellationToken cancellationToken) =>
        await users.GetByIdAsync(request.UserId);
}

public sealed record CheckUserCedulaQuery(string Cedula, string? ExcludingUserId = null) : IRequest<bool>;

public sealed class CheckUserCedulaQueryHandler(IUserReadOnlyService users)
    : IRequestHandler<CheckUserCedulaQuery, bool>
{
    public Task<bool> Handle(CheckUserCedulaQuery request, CancellationToken cancellationToken) =>
        users.ExistsByCedulaAsync(request.Cedula, request.ExcludingUserId);
}

public sealed record GetActiveClientsQuery(string? Cedula) : IRequest<IEnumerable<UserDto>>;

public sealed class GetActiveClientsQueryHandler(IUserReadOnlyService users)
    : IRequestHandler<GetActiveClientsQuery, IEnumerable<UserDto>>
{
    public Task<IEnumerable<UserDto>> Handle(GetActiveClientsQuery request, CancellationToken cancellationToken) =>
        users.GetActiveClientsAsync(request.Cedula);
}

public sealed record GetAdminSavingsAccountsQuery(
    int Page,
    int PageSize,
    AccountStatus? Status,
    AccountType? Type,
    string? Cedula) : IRequest<PaginatedResult<SavingsAccountDto>>;

public sealed class GetAdminSavingsAccountsQueryHandler(ISavingsAccountService accounts)
    : IRequestHandler<GetAdminSavingsAccountsQuery, PaginatedResult<SavingsAccountDto>>
{
    public Task<PaginatedResult<SavingsAccountDto>> Handle(GetAdminSavingsAccountsQuery request, CancellationToken cancellationToken) =>
        accounts.GetAllPagedAsync(request.Page, request.PageSize, request.Status, request.Type, request.Cedula);
}

public sealed record GetAdminSavingsAccountQuery(string AccountNumber) : IRequest<SavingsAccountDto?>;

public sealed class GetAdminSavingsAccountQueryHandler(ISavingsAccountService accounts)
    : IRequestHandler<GetAdminSavingsAccountQuery, SavingsAccountDto?>
{
    public Task<SavingsAccountDto?> Handle(GetAdminSavingsAccountQuery request, CancellationToken cancellationToken) =>
        accounts.GetByAccountNumberAsync(request.AccountNumber);
}

public sealed record GetPrimarySavingsAccountQuery(string ClientId) : IRequest<SavingsAccountDto?>;

public sealed class GetPrimarySavingsAccountQueryHandler(ISavingsAccountService accounts)
    : IRequestHandler<GetPrimarySavingsAccountQuery, SavingsAccountDto?>
{
    public Task<SavingsAccountDto?> Handle(GetPrimarySavingsAccountQuery request, CancellationToken cancellationToken) =>
        accounts.GetPrimaryAccountByClientIdAsync(request.ClientId);
}

public sealed record GetAdminSavingsAccountTransactionsQuery(string AccountNumber)
    : IRequest<AdminSavingsAccountTransactionsResult?>;

public sealed record AdminSavingsAccountTransactionsResult(
    SavingsAccountDto Account,
    IEnumerable<TransactionDto> Transactions);

public sealed class GetAdminSavingsAccountTransactionsQueryHandler(ISavingsAccountService accounts)
    : IRequestHandler<GetAdminSavingsAccountTransactionsQuery, AdminSavingsAccountTransactionsResult?>
{
    public async Task<AdminSavingsAccountTransactionsResult?> Handle(
        GetAdminSavingsAccountTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetByAccountNumberAsync(request.AccountNumber);
        if (account is null)
            return null;

        var transactions = await accounts.GetTransactionsAsync(request.AccountNumber);
        return new AdminSavingsAccountTransactionsResult(account, transactions);
    }
}

public sealed record GetAdminCreditCardsQuery(
    int Page,
    int PageSize,
    CardStatus? Status,
    string? Cedula) : IRequest<PaginatedResult<CreditCardDto>>;

public sealed class GetAdminCreditCardsQueryHandler(ICreditCardService cards)
    : IRequestHandler<GetAdminCreditCardsQuery, PaginatedResult<CreditCardDto>>
{
    public Task<PaginatedResult<CreditCardDto>> Handle(GetAdminCreditCardsQuery request, CancellationToken cancellationToken) =>
        cards.GetAllPagedAsync(request.Page, request.PageSize, request.Status, request.Cedula);
}

public sealed record GetAdminCreditCardDetailsQuery(int CardId) : IRequest<AdminCreditCardDetailsResult?>;

public sealed record AdminCreditCardDetailsResult(
    CreditCardDto Card,
    IEnumerable<CreditCardConsumptionDto> Consumptions);

public sealed class GetAdminCreditCardDetailsQueryHandler(
    ICreditCardService cards,
    ICreditCardConsumptionService consumptions)
    : IRequestHandler<GetAdminCreditCardDetailsQuery, AdminCreditCardDetailsResult?>
{
    public async Task<AdminCreditCardDetailsResult?> Handle(
        GetAdminCreditCardDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var card = await cards.GetByIdAsync(request.CardId);
        if (card is null)
            return null;

        var cardConsumptions = await consumptions.GetByCardIdAsync(request.CardId);
        return new AdminCreditCardDetailsResult(card, cardConsumptions);
    }
}

public sealed record GetAdminLoansQuery(
    int Page,
    int PageSize,
    LoanStatus? Status,
    string? Cedula) : IRequest<PaginatedResult<LoanDto>>;

public sealed class GetAdminLoansQueryHandler(ILoanService loans)
    : IRequestHandler<GetAdminLoansQuery, PaginatedResult<LoanDto>>
{
    public Task<PaginatedResult<LoanDto>> Handle(GetAdminLoansQuery request, CancellationToken cancellationToken) =>
        loans.GetAllPagedAsync(request.Page, request.PageSize, request.Status, request.Cedula);
}

public sealed record GetAdminLoanDetailsQuery(int LoanId) : IRequest<AdminLoanDetailsResult?>;

public sealed record GetAdminLoanQuery(int LoanId) : IRequest<LoanDto?>;

public sealed class GetAdminLoanQueryHandler(ILoanService loans)
    : IRequestHandler<GetAdminLoanQuery, LoanDto?>
{
    public async Task<LoanDto?> Handle(GetAdminLoanQuery request, CancellationToken cancellationToken) =>
        await loans.GetByIdAsync(request.LoanId);
}

public sealed record AdminLoanDetailsResult(
    LoanDto Loan,
    IEnumerable<LoanInstallmentDto> Installments);

public sealed class GetAdminLoanDetailsQueryHandler(
    ILoanService loans,
    ILoanInstallmentService installments)
    : IRequestHandler<GetAdminLoanDetailsQuery, AdminLoanDetailsResult?>
{
    public async Task<AdminLoanDetailsResult?> Handle(
        GetAdminLoanDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var loan = await loans.GetByIdAsync(request.LoanId);
        if (loan is null)
            return null;

        var loanInstallments = await installments.GetByLoanIdAsync(request.LoanId);
        return new AdminLoanDetailsResult(loan, loanInstallments);
    }
}

public sealed record GetAdminLoanAssignmentOptionsQuery(string? Cedula)
    : IRequest<AdminLoanAssignmentOptionsResult>;

public sealed record AdminLoanAssignmentOptionsResult(
    IEnumerable<UserDto> Clients,
    decimal AverageDebt);

public sealed class GetAdminLoanAssignmentOptionsQueryHandler(ILoanService loans)
    : IRequestHandler<GetAdminLoanAssignmentOptionsQuery, AdminLoanAssignmentOptionsResult>
{
    public async Task<AdminLoanAssignmentOptionsResult> Handle(
        GetAdminLoanAssignmentOptionsQuery request,
        CancellationToken cancellationToken)
    {
        var clients = await loans.GetActiveClientsWithoutLoanAsync(request.Cedula);
        var averageDebt = await loans.GetAverageDebtAsync();
        return new AdminLoanAssignmentOptionsResult(clients, averageDebt);
    }
}

public sealed record GetClientActiveLoanQuery(string ClientId) : IRequest<bool>;

public sealed class GetClientActiveLoanQueryHandler(ILoanService loans)
    : IRequestHandler<GetClientActiveLoanQuery, bool>
{
    public Task<bool> Handle(GetClientActiveLoanQuery request, CancellationToken cancellationToken) =>
        loans.ClientHasActiveLoanAsync(request.ClientId);
}

public sealed record EvaluateLoanRiskQuery(
    string ClientId,
    decimal Amount,
    decimal AnnualRate,
    int TermInMonths)
    : IRequest<(bool IsHighRisk, decimal AverageDebt, decimal CurrentDebt)>;

public sealed class EvaluateLoanRiskQueryHandler(ILoanService loans)
    : IRequestHandler<EvaluateLoanRiskQuery, (bool IsHighRisk, decimal AverageDebt, decimal CurrentDebt)>
{
    public Task<(bool IsHighRisk, decimal AverageDebt, decimal CurrentDebt)> Handle(
        EvaluateLoanRiskQuery request,
        CancellationToken cancellationToken) =>
        loans.EvaluateRiskAsync(request.ClientId, request.Amount, request.AnnualRate, request.TermInMonths);
}

public sealed record GetAdminCommercesQuery(int Page, int PageSize, bool? IsActive = null)
    : IRequest<PaginatedResult<CommerceDto>>;

public sealed class GetAdminCommercesQueryHandler(ICommerceService commerces)
    : IRequestHandler<GetAdminCommercesQuery, PaginatedResult<CommerceDto>>
{
    public Task<PaginatedResult<CommerceDto>> Handle(GetAdminCommercesQuery request, CancellationToken cancellationToken) =>
        commerces.GetAllPagedAsync(request.Page, request.PageSize, request.IsActive);
}

public sealed record GetAdminCommerceQuery(int CommerceId) : IRequest<CommerceDto?>;

public sealed class GetAdminCommerceQueryHandler(ICommerceService commerces)
    : IRequestHandler<GetAdminCommerceQuery, CommerceDto?>
{
    public async Task<CommerceDto?> Handle(GetAdminCommerceQuery request, CancellationToken cancellationToken) =>
        await commerces.GetByIdAsync(request.CommerceId);
}
