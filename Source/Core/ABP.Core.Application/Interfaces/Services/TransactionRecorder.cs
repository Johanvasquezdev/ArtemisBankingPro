using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;

namespace ABP.Core.Application.Interfaces.Services
{
    public class TransactionRecorder(ITransactionRepository transactionRepository, IDateTimeProvider dateTimeProvider) : ITransactionRecorder
    {
        private readonly ITransactionRepository _transactionRepository = transactionRepository;
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

        public async Task RecordAsync(TransactionEntry entry)
        {
            var now = _dateTimeProvider.UtcNow;

            await _transactionRepository.AddAsync(new Transaction
            {
                Amount = entry.Amount,
                TransactionDate = now,
                CreatedAt = now,
                Type = entry.Type,
                Origin = entry.Origin,
                Beneficiary = entry.Beneficiary,
                SourceAccountNumber = entry.SourceAccountNumber,
                DestinationAccountNumber = entry.DestinationAccountNumber,
                Description = entry.Description,
                SavingAccountId = entry.SavingAccountId,
                Status = entry.Status
            });
        }

        public async Task RecordDoubleEntryAsync(TransactionEntry debit, TransactionEntry credit)
        {
            await RecordAsync(debit);
            await RecordAsync(credit);
        }
    }
}
