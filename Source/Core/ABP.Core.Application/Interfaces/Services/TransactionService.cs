using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Interfaces;
using ABP.Core.Application.Interfaces.Services;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace ABP.Core.Application.Interfaces.Services;

/// <summary>
/// Backward-compatible facade for legacy callers.
/// New application features depend on ITransactionQueryService, IClientTransactionService,
/// or ICashierTransactionService instead of this aggregate contract.
/// </summary>
public sealed class TransactionService : ITransactionService
{
    private readonly ITransactionQueryService _queries;
    private readonly IClientTransactionService _clientOperations;
    private readonly ICashierTransactionService _cashierOperations;

    public TransactionService(
        ITransactionQueryService queries,
        IClientTransactionService clientOperations,
        ICashierTransactionService cashierOperations)
    {
        _queries = queries;
        _clientOperations = clientOperations;
        _cashierOperations = cashierOperations;
    }

    public TransactionService(
        ITransactionRepository repo,
        ISavingsAccountRepository accountRepo,
        IMapper mapper,
        IUserReadOnlyService user,
        IEmailServices email,
        ICreditCardRepository creditCard,
        ILoanRepository loanRepo,
        ILoanInstallmentRepository installmentRepo,
        IBeneficiaryRepository beneficiaryRepo,
        ICreditCardConsumptionRepository consumptionRepo,
        ITransactionRecorder transactionRecorder,
        IOverpaymentCalculator overpaymentCalculator,
        ILoanPaymentAllocationService loanPaymentAllocationService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger<TransactionService> logger,
        IIdempotencyRepository? idempotencyRepository = null)
    {
        var dependencies = new TransactionOperationDependencies(
            repo,
            accountRepo,
            user,
            email,
            creditCard,
            loanRepo,
            installmentRepo,
            beneficiaryRepo,
            consumptionRepo,
            transactionRecorder,
            overpaymentCalculator,
            loanPaymentAllocationService,
            dateTimeProvider,
            unitOfWork,
            logger,
            idempotencyRepository);

        _queries = new TransactionQueryService(repo, mapper);
        _clientOperations = new ClientTransactionService(dependencies);
        _cashierOperations = new CashierTransactionService(dependencies);
    }

    public Task<TransactionDto> GetByIdAsync(int id) => _queries.GetByIdAsync(id);
    public Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int savingsAccountId) => _queries.GetByAccountIdAsync(savingsAccountId);
    public Task<IEnumerable<TransactionDto>> GetByAccountIdsAsync(IEnumerable<int> savingsAccountIds) => _queries.GetByAccountIdsAsync(savingsAccountIds);
    public Task<IEnumerable<TransactionDto>> GetHistoryAsync(int take = 100) => _queries.GetHistoryAsync(take);
    public Task<int> GetTodayTransactionsCountAsync() => _queries.GetTodayTransactionsCountAsync();
    public Task<int> GetTotalTransactionsCountAsync() => _queries.GetTotalTransactionsCountAsync();
    public Task<int> GetTodayPaymentsCountAsync() => _queries.GetTodayPaymentsCountAsync();
    public Task<int> GetTotalPaymentsCountAsync() => _queries.GetTotalPaymentsCountAsync();

    public Task<CommandResult> MakeExpressTransactionAsync(MakeExpressTransactionDto dto)
        => _clientOperations.MakeExpressTransactionAsync(dto);
    public Task<CommandResult> PayCreditCardAsync(PayCreditCardDto dto)
        => _clientOperations.PayCreditCardAsync(dto);
    public Task<CommandResult> PayLoanAsync(PayLoanDto dto)
        => _clientOperations.PayLoanAsync(dto);
    public Task<CommandResult> PayBeneficiaryAsync(PayBeneficiaryDto dto)
        => _clientOperations.PayBeneficiaryAsync(dto);
    public Task<CommandResult> TransferOwnAccountsAsync(TransferOwnAccountsDto dto)
        => _clientOperations.TransferOwnAccountsAsync(dto);
    public Task<CommandResult> CashAdvanceAsync(CashAdvanceDto dto)
        => _clientOperations.CashAdvanceAsync(dto);

    public Task DepositAsync(CashierDepositDto dto) => _cashierOperations.DepositAsync(dto);
    public Task WithdrawAsync(CashierWithdrawalDto dto) => _cashierOperations.WithdrawAsync(dto);
    public Task CashierPayCreditCardAsync(CashierPayCreditCardDto dto) => _cashierOperations.CashierPayCreditCardAsync(dto);
    public Task CashierPayLoanAsync(CashierPayLoanDto dto) => _cashierOperations.CashierPayLoanAsync(dto);
    public Task CashierTransferAsync(CashierTransferDto dto) => _cashierOperations.CashierTransferAsync(dto);
}
