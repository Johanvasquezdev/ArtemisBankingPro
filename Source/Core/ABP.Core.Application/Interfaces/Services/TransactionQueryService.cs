using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using AutoMapper;

namespace ABP.Core.Application.Interfaces.Services;

internal sealed class TransactionQueryService : ITransactionQueryService
{
    private readonly ITransactionRepository _repo;
    private readonly IMapper _mapper;

    public TransactionQueryService(ITransactionRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<TransactionDto> GetByIdAsync(int id)
    {
        var entity = await _repo.GetByIdAsync(id);
        if (entity is null)
            return new TransactionDto();

        return MapWithFallback(entity);
    }

    public async Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int savingsAccountId)
        => MapWithFallback(await _repo.GetByAccountIdAsync(savingsAccountId));

    public async Task<IEnumerable<TransactionDto>> GetByAccountIdsAsync(IEnumerable<int> savingsAccountIds)
        => MapWithFallback(await _repo.GetByAccountIdsAsync(savingsAccountIds));

    public async Task<IEnumerable<TransactionDto>> GetHistoryAsync(int take = 100)
        => MapWithFallback(await _repo.GetRecentAsync(take));

    public Task<int> GetTodayTransactionsCountAsync()
        => _repo.GetTodayTransactionsCountAsync();

    public Task<int> GetTotalTransactionsCountAsync()
        => _repo.GetTotalTransactionsCountAsync();

    public Task<int> GetTodayPaymentsCountAsync()
        => _repo.GetTodayPaymentsCountAsync();

    public Task<int> GetTotalPaymentsCountAsync()
        => _repo.GetTotalPaymentsCountAsync();

    private TransactionDto MapWithFallback(Transaction entity)
    {
        var dto = _mapper.Map<TransactionDto>(entity);
        if (dto.TransactionDate == default)
            dto.TransactionDate = dto.CreatedAt;

        return dto;
    }

    private IEnumerable<TransactionDto> MapWithFallback(IEnumerable<Transaction> entities)
        => entities.Select(MapWithFallback).ToList();
}
