using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ABP.Integration.Tests.Infrastructure;

public sealed class PersistenceInfrastructureTests : IDisposable
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private readonly ArtemisBankingDbContext _context;

    public PersistenceInfrastructureTests()
    {
        var options = new DbContextOptionsBuilder<ArtemisBankingDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        _context = new ArtemisBankingDbContext(options);
    }

    private ArtemisBankingDbContext CreateVerificationContext()
    {
        var options = new DbContextOptionsBuilder<ArtemisBankingDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        return new ArtemisBankingDbContext(options);
    }

    [Fact]
    public void Model_ShouldUseCurrentSchemaAndFinancialUniquenessConstraints()
    {
        _context.Model.FindEntityType(typeof(Commerce))!.GetSchema()
            .Should().Be("artemisBankingPro");

        var commerceIndexes = _context.Model.FindEntityType(typeof(Commerce))!
            .GetIndexes()
            .Where(index => index.IsUnique)
            .Select(index => string.Join(',', index.Properties.Select(property => property.Name)))
            .ToList();

        commerceIndexes.Should().Contain(["Rnc", "Email"]);

        var idempotencyIndex = _context.Model.FindEntityType(typeof(IdempotencyRecord))!
            .GetIndexes()
            .Single(index => index.IsUnique);

        idempotencyIndex.Properties.Select(property => property.Name)
            .Should().Equal("Operation", "Key", "ActorUserId");
    }

    [Fact]
    public async Task GenericRepository_ShouldStageChangesUntilUnitOfWorkFlushes()
    {
        var repository = new GenericRepository<Commerce>(_context);
        var commerce = new Commerce
        {
            Name = "Staged Commerce",
            Description = "Persistence test",
            Logo = "logo.svg",
            Rnc = "101010101",
            Email = "staged@test.local",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await repository.AddAsync(commerce);

        (await _context.Commerces.AsNoTracking().CountAsync()).Should().Be(0);

        await new UnitOfWork(_context).SaveChangesAsync();

        (await _context.Commerces.AsNoTracking().SingleAsync()).Email
            .Should().Be("staged@test.local");
    }

    [Fact]
    public async Task GenericRepository_WithoutSaveMethods_ShouldOnlyStageChanges()
    {
        var repository = new GenericRepository<Commerce>(_context);
        var commerce = new Commerce
        {
            Name = "Deferred Commerce",
            Description = "Persistence test",
            Logo = "logo.svg",
            Rnc = "111111111",
            Email = "deferred@test.local",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await repository.AddWithoutSaveAsync(commerce);

        _context.Entry(commerce).State.Should().Be(EntityState.Added);
        await using (var verificationContext = CreateVerificationContext())
        {
            (await verificationContext.Commerces.AsNoTracking().CountAsync()).Should().Be(0);
        }

        await new UnitOfWork(_context).SaveChangesAsync();

        await repository.DeleteWithoutSaveAsync(commerce);

        _context.Entry(commerce).State.Should().Be(EntityState.Deleted);
        await using (var verificationContext = CreateVerificationContext())
        {
            (await verificationContext.Commerces.AsNoTracking().AnyAsync()).Should().BeTrue();
        }

        await new UnitOfWork(_context).SaveChangesAsync();
        await using (var verificationContext = CreateVerificationContext())
        {
            (await verificationContext.Commerces.AsNoTracking().AnyAsync()).Should().BeFalse();
        }
    }

    [Fact]
    public async Task GenericRepository_ReadMethods_ShouldReturnPersistedEntities()
    {
        var first = new Commerce
        {
            Name = "First Commerce",
            Description = "Persistence test",
            Logo = "logo.svg",
            Rnc = "404040404",
            Email = "first@test.local",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        var second = new Commerce
        {
            Name = "Second Commerce",
            Description = "Persistence test",
            Logo = "logo.svg",
            Rnc = "505050505",
            Email = "second@test.local",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await _context.Commerces.AddRangeAsync(first, second);
        await _context.SaveChangesAsync();

        var repository = new GenericRepository<Commerce>(_context);

        repository.GetAll().Should().HaveCount(2);
        (await repository.GetAllAsync()).Should().HaveCount(2);
        (await repository.GetByIdAsync(first.Id))!.Email.Should().Be("first@test.local");
        (await repository.GetByIdAsync(999999)).Should().BeNull();
    }

    [Fact]
    public async Task UnitOfWork_ShouldPersistUpdatesAndDeletesAsOneFlush()
    {
        var commerce = new Commerce
        {
            Name = "Original",
            Description = "Persistence test",
            Logo = "logo.svg",
            Rnc = "202020202",
            Email = "original@test.local",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _context.Commerces.AddAsync(commerce);
        await _context.SaveChangesAsync();

        commerce.Name = "Updated";
        var repository = new GenericRepository<Commerce>(_context);
        await repository.UpdateAsync(commerce);
        await new UnitOfWork(_context).SaveChangesAsync();

        (await _context.Commerces.AsNoTracking().SingleAsync()).Name.Should().Be("Updated");

        await repository.DeleteAsync(commerce);
        await new UnitOfWork(_context).SaveChangesAsync();

        (await _context.Commerces.AsNoTracking().AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task GenericRepository_ShouldMergeDetachedUpdateIntoTrackedEntity()
    {
        var commerce = new Commerce
        {
            Name = "Tracked",
            Description = "Persistence test",
            Logo = "logo.svg",
            Rnc = "303030303",
            Email = "tracked@test.local",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };
        await _context.Commerces.AddAsync(commerce);
        await _context.SaveChangesAsync();

        var detached = await _context.Commerces.AsNoTracking().SingleAsync();
        detached.Name = "Updated from detached instance";

        await new GenericRepository<Commerce>(_context).UpdateWithoutSaveAsync(detached);
        await new UnitOfWork(_context).SaveChangesAsync();

        (await _context.Commerces.AsNoTracking().SingleAsync()).Name
            .Should().Be("Updated from detached instance");
    }

    [Fact]
    public void FinancialEntities_ShouldExposeRequiredStatusAndMoneyMetadata()
    {
        var card = _context.Model.FindEntityType(typeof(CreditCard))!;
        card.FindProperty(nameof(CreditCard.CreditLimit))!.GetPrecision().Should().Be(18);
        card.FindProperty(nameof(CreditCard.CreditLimit))!.GetScale().Should().Be(2);
        card.FindProperty(nameof(CreditCard.Status))!.IsNullable.Should().BeFalse();

        var installment = _context.Model.FindEntityType(typeof(LoanInstallment))!;
        installment.FindProperty(nameof(LoanInstallment.PrincipalPortion))!.IsNullable.Should().BeFalse();
        installment.FindProperty(nameof(LoanInstallment.InterestPortion))!.IsNullable.Should().BeFalse();
        installment.FindProperty(nameof(LoanInstallment.IsOverdue))!.IsNullable.Should().BeFalse();
    }

    public void Dispose() => _context.Dispose();
}
