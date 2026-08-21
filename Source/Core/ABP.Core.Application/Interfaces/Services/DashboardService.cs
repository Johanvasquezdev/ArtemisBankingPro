using AutoMapper;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Dashboard;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ABP.Core.Application.Interfaces.Services
{
    public class DashboardService( ITransactionRepository transactionRepo, ISavingsAccountRepository accountRepo, ICreditCardRepository cardRepo,
        ILoanRepository loanRepo,
        IUserReadOnlyService userService,
        IMapper mapper,
        IMemoryCache cache) : IDashboardService
    {
        private readonly ITransactionRepository _transactionRepo = transactionRepo;
        private readonly ISavingsAccountRepository _accountRepo = accountRepo;
        private readonly ICreditCardRepository _cardRepo = cardRepo;
        private readonly ILoanRepository _loanRepo = loanRepo;
        private readonly IUserReadOnlyService _userService = userService;
        private readonly IMapper _mapper = mapper;
        private readonly IMemoryCache _cache = cache;
        private const string AdminDashboardCacheKey = "dashboard:admin";

        public async Task<DashboardAdminDto> GetAdminDashboardAsync()
        {
            var dashboard = await _cache.GetOrCreateAsync(AdminDashboardCacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(15);
                return BuildAdminDashboardAsync();
            });

            return dashboard!;
        }

        private async Task<DashboardAdminDto> BuildAdminDashboardAsync()
        {
            var activeAccounts = await _accountRepo.GetTotalActiveAccountsCountAsync();
            var activeCards = await _cardRepo.GetTotalActiveCardsCountAsync();
            var activeLoans = await _loanRepo.GetTotalActiveLoansCountAsync();
            var activeClients = await _userService.GetActiveClientsCountAsync();
            var inactiveClients = await _userService.GetInactiveClientsCountAsync();
            return new DashboardAdminDto
            {
                TotalTransactions = await _transactionRepo.GetTotalTransactionsCountAsync(),
                TodayTransactions = await _transactionRepo.GetTodayTransactionsCountAsync(),
                TodayPayments = await _transactionRepo.GetTodayPaymentsCountAsync(),
                TotalPayments = await _transactionRepo.GetTotalPaymentsCountAsync(),
                ActiveClients = activeClients,
                InactiveClients = inactiveClients,
                TotalProducts = activeAccounts + activeCards + activeLoans,
                ActiveLoans = activeLoans,
                ActiveCreditCards = activeCards,
                TotalSavingsAccounts = activeAccounts,
                AverageDebt = await _loanRepo.GetAverageDebtAsync()
            };
        }

        public async Task<DashboardClientDto> GetClientDashboardAsync(string clientId)
        {
            var accounts = await _accountRepo.GetAllAccountByClienteIdAsync(clientId);
            var cards = await _cardRepo.GetActiveCardsByClientIdAsync(clientId);
            var loans = await _loanRepo.GetActiveByClientIdAsync(clientId);

            return new DashboardClientDto
            {
                TotalSavingsAccounts = accounts.Count(),
                TotalCreditCards = cards.Count(),
                TotalLoans = loans.Count(),
                SavingsAccounts = _mapper.Map<IEnumerable<SavingsAccountDto>>(accounts),
                CreditCards = _mapper.Map<IEnumerable<CreditCardDto>>(cards),
                Loans = _mapper.Map<IEnumerable<LoanDto>>(loans)
            };
        }

        public async Task<DashboardCashierDto> GetCashierDashboardAsync(string cashierId)
        {
            var recentTransactions = await _transactionRepo.GetRecentByPerformerIdAsync(cashierId, 3);
            return new DashboardCashierDto
            {
                TodayTransactions = await _transactionRepo.GetTodayTransactionsByUserIdCountAsync(cashierId),
                TodayPayments = await _transactionRepo.GetTodayPaymentsByUserIdCountAsync(cashierId),
                TodayDeposits = await _transactionRepo.GetTodayDepositsAmountByUserIdAsync(cashierId),
                TodayWithdrawals = await _transactionRepo.GetTodayWithdrawalsAmountByUserIdAsync(cashierId),
                RecentTransactions = _mapper.Map<IReadOnlyList<ABP.Core.Application.DTOs.Transaction.TransactionDto>>(recentTransactions)
            };
        }
    }
}
