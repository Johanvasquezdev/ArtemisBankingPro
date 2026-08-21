using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;
using Xunit;

namespace ABP.Integration.Tests.Repositories;

public sealed class TransactionRepositoryConstraintsTests : IDisposable
{
    private readonly ArtemisBankingDbContext _dbContext;
    private readonly TransactionRepository _repo;

    public TransactionRepositoryConstraintsTests()
    {
        var options = new DbContextOptionsBuilder<ArtemisBankingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        _dbContext = new ArtemisBankingDbContext(options);
        _dbContext.Database.EnsureCreated();
        _repo = new TransactionRepository(_dbContext);
    }

    [Fact]
    public async Task AddAsync_ShouldThrowInvalidOperationException_WhenPerformedByUserIdIsNull()
    {
        var tx = new Transaction
        {
            Amount = 100,
            Type = TransactionType.Credit,
            PerformedByUserId = null
        };

        var act = async () => await _repo.AddAsync(tx);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*PerformedByUserId*");
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }
}
