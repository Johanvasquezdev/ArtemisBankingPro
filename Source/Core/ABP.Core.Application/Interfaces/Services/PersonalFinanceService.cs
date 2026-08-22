using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;

namespace ABP.Core.Application.Interfaces.Services
{
    public class PersonalFinanceService : IPersonalFinanceService
    {
        private readonly ITransactionRepository _transactionRepository;

        public PersonalFinanceService(ITransactionRepository transactionRepository)
        {
            _transactionRepository = transactionRepository;
        }

        public async Task<Dictionary<string, decimal>> GetExpensesByCategoryAsync(string clientId, int month, int year)
        {
            var allTransactions = await _transactionRepository.GetAllAsync();
            var expenses = allTransactions
                .Where(t => t.PerformedByUserId == clientId
                         && (t.Type == TransactionType.Debit || t.Type == TransactionType.Payment)
                         && t.Status == TransactionStatus.Approved
                         && t.TransactionDate.Month == month
                         && t.TransactionDate.Year == year)
                .GroupBy(t => t.Category)
                .Select(g => new { Category = g.Key.ToString(), Total = g.Sum(t => t.Amount) })
                .ToDictionary(k => k.Category, v => v.Total);

            return expenses;
        }
    }
}


