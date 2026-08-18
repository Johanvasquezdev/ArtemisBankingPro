using ABP.Core.Domain.Interfaces;
using ABP.Core.Application.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace ABP.Core.Application.Interfaces.Services;

internal sealed class TransactionOperationDependencies
{
    public TransactionOperationDependencies(
        ITransactionRepository transactionRepository,
        ISavingsAccountRepository savingsAccountRepository,
        IUserReadOnlyService userService,
        IEmailServices emailService,
        ICreditCardRepository creditCardRepository,
        ILoanRepository loanRepository,
        ILoanInstallmentRepository loanInstallmentRepository,
        IBeneficiaryRepository beneficiaryRepository,
        ICreditCardConsumptionRepository consumptionRepository,
        ITransactionRecorder transactionRecorder,
        IOverpaymentCalculator overpaymentCalculator,
        ILoanPaymentAllocationService loanPaymentAllocationService,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork,
        ILogger logger,
        IIdempotencyRepository? idempotencyRepository)
    {
        TransactionRepository = transactionRepository;
        SavingsAccountRepository = savingsAccountRepository;
        UserService = userService;
        EmailService = emailService;
        CreditCardRepository = creditCardRepository;
        LoanRepository = loanRepository;
        LoanInstallmentRepository = loanInstallmentRepository;
        BeneficiaryRepository = beneficiaryRepository;
        ConsumptionRepository = consumptionRepository;
        TransactionRecorder = transactionRecorder;
        OverpaymentCalculator = overpaymentCalculator;
        LoanPaymentAllocationService = loanPaymentAllocationService;
        DateTimeProvider = dateTimeProvider;
        UnitOfWork = unitOfWork;
        Logger = logger;
        IdempotencyRepository = idempotencyRepository;
    }

    public ITransactionRepository TransactionRepository { get; }
    public ISavingsAccountRepository SavingsAccountRepository { get; }
    public IUserReadOnlyService UserService { get; }
    public IEmailServices EmailService { get; }
    public ICreditCardRepository CreditCardRepository { get; }
    public ILoanRepository LoanRepository { get; }
    public ILoanInstallmentRepository LoanInstallmentRepository { get; }
    public IBeneficiaryRepository BeneficiaryRepository { get; }
    public ICreditCardConsumptionRepository ConsumptionRepository { get; }
    public ITransactionRecorder TransactionRecorder { get; }
    public IOverpaymentCalculator OverpaymentCalculator { get; }
    public ILoanPaymentAllocationService LoanPaymentAllocationService { get; }
    public IDateTimeProvider DateTimeProvider { get; }
    public IUnitOfWork UnitOfWork { get; }
    public ILogger Logger { get; }
    public IIdempotencyRepository? IdempotencyRepository { get; }
}
