using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;

namespace ABP.Core.Application.Interfaces.Services
{
    public class TransactionRecorder(
        ITransactionRepository transactionRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork) : ITransactionRecorder
    {
        private readonly ITransactionRepository _transactionRepository = transactionRepository;
        private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task RecordAsync(TransactionEntry entry)
        {
            var now = _dateTimeProvider.UtcNow;

            await _transactionRepository.AddWithoutSaveAsync(new Transaction
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
                Status = entry.Status,
                PerformedByUserId = entry.PerformedByUserId
            });
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RecordWithoutSaveAsync(TransactionEntry entry)
        {
            var now = _dateTimeProvider.UtcNow;

            await _transactionRepository.AddWithoutSaveAsync(new Transaction
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
                Status = entry.Status,
                PerformedByUserId = entry.PerformedByUserId
            });
        }

        public async Task RecordDoubleEntryAsync(TransactionEntry debit, TransactionEntry credit)
        {
            await RecordWithoutSaveAsync(debit);
            await RecordWithoutSaveAsync(credit);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task RecordDoubleEntryWithoutSaveAsync(TransactionEntry debit, TransactionEntry credit)
        {
            await RecordWithoutSaveAsync(debit);
            await RecordWithoutSaveAsync(credit);
        }
    }
}
