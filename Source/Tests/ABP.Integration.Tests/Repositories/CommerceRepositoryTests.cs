using ABP.Core.Domain.Entities;
using ABP.Infraestructure.identity.Context;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ABP.Integration.Tests.Repositories;

public sealed class CommerceRepositoryTests : IDisposable
{
    private readonly ArtemisBankingDbContext _dbContext;
    private readonly CommerceRepository _repository;

    public CommerceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ArtemisBankingDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;

        _dbContext = new ArtemisBankingDbContext(options);
        _dbContext.Database.EnsureCreated();

        _repository = new CommerceRepository(_dbContext);
    }

    [Fact]
    public async Task GetAllPagedAsync_ShouldFilterActiveStatusAndPage()
    {
        await _dbContext.Commerces.AddRangeAsync(
            Commerce(1, "111111111", "one@test.local", true, DateTime.UtcNow.AddMinutes(-2)),
            Commerce(2, "222222222", "two@test.local", false, DateTime.UtcNow.AddMinutes(-1)),
            Commerce(3, "333333333", "three@test.local", true, DateTime.UtcNow));
        await _dbContext.SaveChangesAsync();

        var result = await _repository.GetAllPagedAsync(page: 1, pageSize: 1, isActive: true);

        result.Should().ContainSingle(commerce => commerce.Id == 3);
    }

    [Fact]
    public async Task ExistsByRncAndEmail_ShouldHonorExclusionId()
    {
        await _dbContext.Commerces.AddAsync(Commerce(1, "444444444", "same@test.local", true, DateTime.UtcNow));
        await _dbContext.SaveChangesAsync();

        (await _repository.ExistsByRncAsync("444444444")).Should().BeTrue();
        (await _repository.ExistsByEmailAsync("same@test.local")).Should().BeTrue();
        (await _repository.ExistsByRncAsync("444444444", excludingId: 1)).Should().BeFalse();
        (await _repository.ExistsByEmailAsync("same@test.local", excludingId: 1)).Should().BeFalse();
    }

    private static Commerce Commerce(int id, string rnc, string email, bool active, DateTime createdAt) => new()
    {
        Id = id,
        Name = $"Commerce {id}",
        Description = "Integration test commerce",
        Rnc = rnc,
        Email = email,
        Logo = "logo.svg",
        IsActive = active,
        CreatedAt = createdAt
    };

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();

    }
}
