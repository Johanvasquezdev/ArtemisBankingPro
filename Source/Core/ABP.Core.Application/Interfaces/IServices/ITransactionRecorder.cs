using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.Interfaces.IServices
{
    /// <summary>
    /// Records the financial history of an operation. Centralizes how transactions
    /// are created so that adding a new transaction type does not require changing
    /// the recording logic (OCP).
    /// </summary>
    public interface ITransactionRecorder
    {
        Task RecordAsync(TransactionEntry entry);
        Task RecordDoubleEntryAsync(TransactionEntry debit, TransactionEntry credit);
    }
}
