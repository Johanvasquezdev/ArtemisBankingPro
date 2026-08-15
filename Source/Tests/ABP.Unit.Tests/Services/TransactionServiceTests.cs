using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Exceptions;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _transactionRepo;
        private readonly Mock<ISavingsAccountRepository> _accountRepo;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<IUserReadOnlyService> _userService;
        private readonly Mock<IEmailServices> _emailService;
        private readonly Mock<ICreditCardRepository> _creditCardRepo;
        private readonly Mock<ILoanRepository> _loanRepo;
        private readonly Mock<ILoanInstallmentRepository> _installmentRepo;
        private readonly Mock<IBeneficiaryRepository> _beneficiaryRepo;
        private readonly Mock<ICreditCardConsumptionRepository> _consumptionRepo;
        private readonly Mock<ITransactionRecorder> _transactionRecorder;
        private readonly Mock<IDateTimeProvider> _dateTimeProvider;
        private readonly Mock<IUnitOfWork> _unitOfWork;
        private readonly Mock<IUnitOfWorkTransaction> _transaction;
        private readonly List<Transaction> _recordedTransactions = [];

        private readonly TransactionService _service;
        private readonly AntiOverpaymentCalculator _overpaymentCalculator = new();
        private readonly LoanPaymentAllocationService _allocationService = new();

        public TransactionServiceTests()
        {
            _transactionRepo = new Mock<ITransactionRepository>();
            _accountRepo = new Mock<ISavingsAccountRepository>();
            _mapper = new Mock<IMapper>();
            _userService = new Mock<IUserReadOnlyService>();
            _emailService = new Mock<IEmailServices>();
            _creditCardRepo = new Mock<ICreditCardRepository>();
            _loanRepo = new Mock<ILoanRepository>();
            _installmentRepo = new Mock<ILoanInstallmentRepository>();
            _beneficiaryRepo = new Mock<IBeneficiaryRepository>();
            _consumptionRepo = new Mock<ICreditCardConsumptionRepository>();
            _transactionRecorder = new Mock<ITransactionRecorder>();
            _dateTimeProvider = new Mock<IDateTimeProvider>();
            _unitOfWork = new Mock<IUnitOfWork>();
            _transaction = new Mock<IUnitOfWorkTransaction>();

            _dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTime(2026, 8, 9, 12, 0, 0));
            _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_transaction.Object);

            _transactionRecorder.Setup(x => x.RecordAsync(It.IsAny<TransactionEntry>()))
                .Callback<TransactionEntry>(entry =>
                {
                    _recordedTransactions.Add(new Transaction
                    {
                        Amount = entry.Amount,
                        Type = entry.Type,
                        Origin = entry.Origin,
                        Beneficiary = entry.Beneficiary,
                        SourceAccountNumber = entry.SourceAccountNumber,
                        DestinationAccountNumber = entry.DestinationAccountNumber,
                        Description = entry.Description,
                        SavingAccountId = entry.SavingAccountId,
                        Status = entry.Status
                    });
                });

            _service = new TransactionService(
                _transactionRepo.Object,
                _accountRepo.Object,
                _mapper.Object,
                _userService.Object,
                _emailService.Object,
                _creditCardRepo.Object,
                _loanRepo.Object,
                _installmentRepo.Object,
                _beneficiaryRepo.Object,
                _consumptionRepo.Object,
                _transactionRecorder.Object,
                _overpaymentCalculator,
                _allocationService,
                _dateTimeProvider.Object,
                _unitOfWork.Object,
                NullLogger<TransactionService>.Instance
            );
        }

        #region Cashier operations (regression)

        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            var request = new CashierDepositDto { AccountNumber = "INVALID", Amount = 100 };
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(request.AccountNumber))
                .ReturnsAsync((SavingsAccount?)null);

            Func<Task> act = async () => await _service.DepositAsync(request);

            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*La cuenta de destino no existe.*");
        }

        [Fact]
        public async Task DepositAsync_ShouldSucceed_WhenAccountExists()
        {
            var request = new CashierDepositDto { AccountNumber = "ACC-1", Amount = 100 };
            var account = new SavingsAccount { Id = 1, AccountNumber = "ACC-1", Balance = 500, Status = AccountStatus.Active };

            _accountRepo.Setup(x => x.GetByAccountNumberAsync(request.AccountNumber)).ReturnsAsync(account);
            _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync((ABP.Core.Application.DTOs.User.UserDto?)null);

            await _service.DepositAsync(request);

            account.Balance.Should().Be(600);
            _accountRepo.Verify(x => x.UpdateAsync(account), Times.Once);
            _transactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Once);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenInsufficientFunds()
        {
            var request = new CashierWithdrawalDto { AccountNumber = "ACC-1", Amount = 1000 };
            var senderAccount = new SavingsAccount { Id = 1, AccountNumber = "ACC-1", Balance = 500, Status = AccountStatus.Active };

            _accountRepo.Setup(x => x.GetByAccountNumberAsync(request.AccountNumber)).ReturnsAsync(senderAccount);

            Func<Task> act = async () => await _service.WithdrawAsync(request);

            await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage($"*Fondos insuficientes. Balance actual: {senderAccount.Balance:C}*");
        }

        #endregion

        #region Express transactions

        [Fact]
        public async Task MakeExpressTransaction_ShouldRecordRejected_WhenInsufficientFunds()
        {
            var source = Account("CLIENT-1", "000000001", 100m);
            var destination = Account("CLIENT-2", "000000002", 0m);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(destination.AccountNumber)).ReturnsAsync(destination);

            var dto = new MakeExpressTransactionDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = destination.AccountNumber,
                Amount = 500m
            };

            Func<Task> act = async () => await _service.MakeExpressTransactionAsync(dto);

            await act.Should().ThrowAsync<AmountExceedsBalanceException>();
            _transactionRecorder.Verify(x => x.RecordAsync(It.Is<TransactionEntry>(e => e.Status == TransactionStatus.Declined)), Times.Once);
            _transactionRecorder.Verify(x => x.RecordDoubleEntryAsync(It.IsAny<TransactionEntry>(), It.IsAny<TransactionEntry>()), Times.Never);
            source.Balance.Should().Be(100m);
            destination.Balance.Should().Be(0m);
        }

        [Fact]
        public async Task MakeExpressTransaction_ShouldTransferAndRecordDoubleEntry_WhenEnoughFunds()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var destination = Account("CLIENT-2", "000000002", 500m);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(destination.AccountNumber)).ReturnsAsync(destination);
            _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ABP.Core.Application.DTOs.User.UserDto { Id = id, Email = $"{id}@test.com" });

            var dto = new MakeExpressTransactionDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = destination.AccountNumber,
                Amount = 200m
            };

            var result = await _service.MakeExpressTransactionAsync(dto);

            result.Succeeded.Should().BeTrue();
            source.Balance.Should().Be(800m);
            destination.Balance.Should().Be(700m);
            _transactionRecorder.Verify(x => x.RecordDoubleEntryAsync(
                It.Is<TransactionEntry>(e => e.Type == TransactionType.Debit && e.Amount == 200m),
                It.Is<TransactionEntry>(e => e.Type == TransactionType.Credit && e.Amount == 200m)), Times.Once);
            _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MakeExpressTransaction_ShouldThrow_WhenSameAccount()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);

            var dto = new MakeExpressTransactionDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = source.AccountNumber,
                Amount = 200m
            };

            Func<Task> act = async () => await _service.MakeExpressTransactionAsync(dto);

            await act.Should().ThrowAsync<SameAccountException>();
        }

        #endregion

        #region Credit card payments

        [Fact]
        public async Task PayCreditCard_ShouldRecordRejected_WhenNoOutstandingDebt()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var card = Card("CLIENT-1", "1111222233334444", 0m, CardStatus.Active);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _creditCardRepo.Setup(x => x.GetByCardNumberAsync(card.CardNumber)).ReturnsAsync(card);

            var dto = new PayCreditCardDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                CreditCardNumber = card.CardNumber,
                Amount = 100m
            };

            Func<Task> act = async () => await _service.PayCreditCardAsync(dto);

            await act.Should().ThrowAsync<NoOutstandingDebtException>();
            _transactionRecorder.Verify(x => x.RecordAsync(It.Is<TransactionEntry>(e => e.Status == TransactionStatus.Declined)), Times.Once);
        }

        [Fact]
        public async Task PayCreditCard_ShouldApplyAntiOverpayment_WhenAmountExceedsDebt()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var card = Card("CLIENT-1", "1111222233334444", 100m, CardStatus.Active);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _creditCardRepo.Setup(x => x.GetByCardNumberAsync(card.CardNumber)).ReturnsAsync(card);
            _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new ABP.Core.Application.DTOs.User.UserDto { Id = "CLIENT-1", Email = "c@test.com" });

            var dto = new PayCreditCardDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                CreditCardNumber = card.CardNumber,
                Amount = 500m
            };

            await _service.PayCreditCardAsync(dto);

            source.Balance.Should().Be(900m);
            card.AmountOwed.Should().Be(0m);
            _transactionRecorder.Verify(x => x.RecordAsync(It.Is<TransactionEntry>(e => e.Amount == 100m && e.Status == TransactionStatus.Approved)), Times.Once);
        }

        #endregion

        #region Cash advance (6.25%)

        [Fact]
        public async Task CashAdvance_ShouldChargeSixPointTwentyFivePercent_OnTotalToCharge()
        {
            var card = Card("CLIENT-1", "1111222233334444", 0m, CardStatus.Active);
            card.CreditLimit = 1000m;
            card.ExpirationDate = "12/30";
            var account = Account("CLIENT-1", "000000001", 500m);
            _creditCardRepo.Setup(x => x.GetByIdAsync(card.Id)).ReturnsAsync(card);
            _accountRepo.Setup(x => x.GetByIdAsync(account.Id)).ReturnsAsync(account);

            var dto = new CashAdvanceDto
            {
                ClientId = "CLIENT-1",
                CreditCardId = card.Id,
                SavingsAccountId = account.Id,
                Amount = 100m
            };

            await _service.CashAdvanceAsync(dto);

            account.Balance.Should().Be(600m);
            card.AmountOwed.Should().Be(106.25m);
            _consumptionRepo.Verify(x => x.AddAsync(It.Is<CreditCardConsumption>(c =>
                c.Amount == 106.25m && c.CommerceName == "AVANCE")), Times.Once);
            _transactionRecorder.Verify(x => x.RecordAsync(It.Is<TransactionEntry>(e =>
                e.Type == TransactionType.Credit && e.Amount == 100m && e.Status == TransactionStatus.Approved)), Times.Once);
        }

        [Fact]
        public async Task CashAdvance_ShouldThrow_WhenTotalToChargeExceedsAvailableCredit()
        {
            var card = Card("CLIENT-1", "1111222233334444", 0m, CardStatus.Active);
            card.CreditLimit = 50m;
            card.ExpirationDate = "12/30";
            var account = Account("CLIENT-1", "000000001", 500m);
            _creditCardRepo.Setup(x => x.GetByIdAsync(card.Id)).ReturnsAsync(card);
            _accountRepo.Setup(x => x.GetByIdAsync(account.Id)).ReturnsAsync(account);

            var dto = new CashAdvanceDto
            {
                ClientId = "CLIENT-1",
                CreditCardId = card.Id,
                SavingsAccountId = account.Id,
                Amount = 100m
            };

            Func<Task> act = async () => await _service.CashAdvanceAsync(dto);

            await act.Should().ThrowAsync<InsufficientAvailableCreditException>();
        }

        [Fact]
        public async Task CashAdvance_ShouldThrow_WhenCardDoesNotBelongToClient()
        {
            var card = Card("OTHER-CLIENT", "1111222233334444", 0m, CardStatus.Active);
            card.ExpirationDate = "12/30";
            var account = Account("CLIENT-1", "000000001", 500m);
            _creditCardRepo.Setup(x => x.GetByIdAsync(card.Id)).ReturnsAsync(card);
            _accountRepo.Setup(x => x.GetByIdAsync(account.Id)).ReturnsAsync(account);

            var dto = new CashAdvanceDto
            {
                ClientId = "CLIENT-1",
                CreditCardId = card.Id,
                SavingsAccountId = account.Id,
                Amount = 100m
            };

            Func<Task> act = async () => await _service.CashAdvanceAsync(dto);

            await act.Should().ThrowAsync<CardNotFoundException>();
        }

        #endregion

        #region Loan payments

        [Fact]
        public async Task PayLoan_ShouldAllocatePaymentBySeniority()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var loan = Loan("CLIENT-1", "LN-000001", LoanStatus.Active);
            var installment1 = new LoanInstallment { Id = 1, LoanId = loan.Id, InstallmentNumber = 1, InstallmentAmount = 300m, AmountPaid = 0, DueDate = new DateTime(2026, 1, 1), Status = InstallmentStatus.Pending };
            var installment2 = new LoanInstallment { Id = 2, LoanId = loan.Id, InstallmentNumber = 2, InstallmentAmount = 300m, AmountPaid = 0, DueDate = new DateTime(2026, 2, 1), Status = InstallmentStatus.Pending };

            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _loanRepo.Setup(x => x.GetByLoanNumberAsync(loan.LoanNumber)).ReturnsAsync(loan);
            _installmentRepo.Setup(x => x.GetByLoanIdAsync(loan.Id)).ReturnsAsync(new[] { installment1, installment2 });
            _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new ABP.Core.Application.DTOs.User.UserDto { Id = "CLIENT-1", Email = "c@test.com" });

            var dto = new PayLoanDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                LoanNumber = loan.LoanNumber,
                Amount = 400m
            };

            await _service.PayLoanAsync(dto);

            source.Balance.Should().Be(600m);
            installment1.AmountPaid.Should().Be(300m);
            installment1.Status.Should().Be(InstallmentStatus.Paid);
            installment2.AmountPaid.Should().Be(100m);
            installment2.Status.Should().Be(InstallmentStatus.Pending);
        }

        [Fact]
        public async Task PayLoan_ShouldCompleteLoan_WhenFullyPaid()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var loan = Loan("CLIENT-1", "LN-000001", LoanStatus.Active);
            var installment1 = new LoanInstallment { Id = 1, LoanId = loan.Id, InstallmentNumber = 1, InstallmentAmount = 300m, AmountPaid = 0, DueDate = new DateTime(2026, 1, 1), Status = InstallmentStatus.Pending };

            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _loanRepo.Setup(x => x.GetByLoanNumberAsync(loan.LoanNumber)).ReturnsAsync(loan);
            _installmentRepo.Setup(x => x.GetByLoanIdAsync(loan.Id)).ReturnsAsync(new[] { installment1 });
            _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new ABP.Core.Application.DTOs.User.UserDto { Id = "CLIENT-1", Email = "c@test.com" });

            var dto = new PayLoanDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                LoanNumber = loan.LoanNumber,
                Amount = 300m
            };

            await _service.PayLoanAsync(dto);

            installment1.Status.Should().Be(InstallmentStatus.Paid);
            loan.Status.Should().Be(LoanStatus.Completed);
            _loanRepo.Verify(x => x.UpdateAsync(loan), Times.Once);
        }

        [Fact]
        public async Task PayLoan_ShouldRecordRejected_WhenNoPendingInstallments()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var loan = Loan("CLIENT-1", "LN-000001", LoanStatus.Active);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _loanRepo.Setup(x => x.GetByLoanNumberAsync(loan.LoanNumber)).ReturnsAsync(loan);
            _installmentRepo.Setup(x => x.GetByLoanIdAsync(loan.Id)).ReturnsAsync([]);

            var dto = new PayLoanDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                LoanNumber = loan.LoanNumber,
                Amount = 100m
            };

            Func<Task> act = async () => await _service.PayLoanAsync(dto);

            await act.Should().ThrowAsync<NoPendingInstallmentsException>();
            _transactionRecorder.Verify(x => x.RecordAsync(It.Is<TransactionEntry>(e => e.Status == TransactionStatus.Declined)), Times.Once);
        }

        #endregion

        #region Beneficiary payments

        [Fact]
        public async Task PayBeneficiary_ShouldTransfer_WhenValid()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var destination = Account("CLIENT-2", "000000002", 100m);
            var beneficiary = new Beneficiary { Id = 1, AccountNumber = destination.AccountNumber, OwnerId = "CLIENT-1" };

            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(destination.AccountNumber)).ReturnsAsync(destination);
            _beneficiaryRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(beneficiary);
            _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => new ABP.Core.Application.DTOs.User.UserDto { Id = id, Email = $"{id}@test.com" });

            var dto = new PayBeneficiaryDto
            {
                ClientId = "CLIENT-1",
                BeneficiaryId = 1,
                SourceAccountNumber = source.AccountNumber,
                Amount = 250m
            };

            await _service.PayBeneficiaryAsync(dto);

            source.Balance.Should().Be(750m);
            destination.Balance.Should().Be(350m);
            _transactionRecorder.Verify(x => x.RecordDoubleEntryAsync(It.IsAny<TransactionEntry>(), It.IsAny<TransactionEntry>()), Times.Once);
        }

        [Fact]
        public async Task PayBeneficiary_ShouldThrow_WhenBeneficiaryNotOwnedByClient()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var beneficiary = new Beneficiary { Id = 1, AccountNumber = "000000002", OwnerId = "OTHER-CLIENT" };
            _accountRepo.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
            _beneficiaryRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(beneficiary);

            var dto = new PayBeneficiaryDto
            {
                ClientId = "CLIENT-1",
                BeneficiaryId = 1,
                SourceAccountNumber = source.AccountNumber,
                Amount = 250m
            };

            Func<Task> act = async () => await _service.PayBeneficiaryAsync(dto);

            await act.Should().ThrowAsync<BeneficiaryNotFoundException>();
        }

        #endregion

        #region Transfers between own accounts

        [Fact]
        public async Task TransferOwnAccounts_ShouldThrow_WhenOnlyOneActiveAccount()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            _accountRepo.Setup(x => x.GetActiveAccountsByClientIdAsync("CLIENT-1")).ReturnsAsync(new[] { source });

            var dto = new TransferOwnAccountsDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = "000000002",
                Amount = 100m
            };

            Func<Task> act = async () => await _service.TransferOwnAccountsAsync(dto);

            await act.Should().ThrowAsync<InsufficientAccountsException>();
        }

        [Fact]
        public async Task TransferOwnAccounts_ShouldTransfer_WhenTwoAccounts()
        {
            var source = Account("CLIENT-1", "000000001", 1000m);
            var destination = Account("CLIENT-1", "000000002", 100m);
            _accountRepo.Setup(x => x.GetActiveAccountsByClientIdAsync("CLIENT-1")).ReturnsAsync(new[] { source, destination });
            _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(new ABP.Core.Application.DTOs.User.UserDto { Id = "CLIENT-1", Email = "c@test.com" });

            var dto = new TransferOwnAccountsDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = source.AccountNumber,
                DestinationAccountNumber = destination.AccountNumber,
                Amount = 300m
            };

            await _service.TransferOwnAccountsAsync(dto);

            source.Balance.Should().Be(700m);
            destination.Balance.Should().Be(400m);
            _transactionRecorder.Verify(x => x.RecordDoubleEntryAsync(It.IsAny<TransactionEntry>(), It.IsAny<TransactionEntry>()), Times.Once);
        }

        #endregion

        private static SavingsAccount Account(string clientId, string number, decimal balance)
            => new()
            {
                Id = int.Parse(number),
                AccountNumber = number,
                Balance = balance,
                Status = AccountStatus.Active,
                UserId = clientId
            };

        private static CreditCard Card(string clientId, string number, decimal amountOwed, CardStatus status)
            => new()
            {
                Id = number.Length,
                CardNumber = number,
                CreditLimit = 1000m,
                AmountOwed = amountOwed,
                ExpirationDate = "12/30",
                Status = status,
                ClientId = clientId
            };

        private static Loan Loan(string clientId, string number, LoanStatus status)
            => new()
            {
                Id = number.Length,
                LoanNumber = number,
                Amount = 1000m,
                Status = status,
                ClientId = clientId
            };
    }
}
