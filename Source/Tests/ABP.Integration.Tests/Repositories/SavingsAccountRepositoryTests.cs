using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ABP.Integration.Tests.Repositories
{
    public class SavingsAccountRepositoryTests : IDisposable
    {
        private readonly ArtemisBankingDbContext _dbContext;
        private readonly SavingsAccountRepository _repository;

        public SavingsAccountRepositoryTests()
        {
            var dbOptions = new DbContextOptionsBuilder<ArtemisBankingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ArtemisBankingDbContext(dbOptions);
            _dbContext.Database.EnsureCreated();

            _repository = new SavingsAccountRepository(_dbContext);
        }

        [Fact]
        public async Task AddAsync_ShouldAddSavingsAccountToDatabase()
        {
            // Arrange
            var account = new SavingsAccount
            {
                UserId = "user_456",
                AccountNumber = "ACC-12345",
                Balance = 500m,
                Type = AccountType.Primary,
                Status = AccountStatus.Active,
                CreatedAt = DateTime.UtcNow,
                CreatedByAdminId = "admin"
            };

            // Act
            await _repository.AddAsync(account);
            await _dbContext.SaveChangesAsync();

            // Assert
            account.Id.Should().BeGreaterThan(0);

            var dbAccount = await _dbContext.Savings.FindAsync(account.Id);
            dbAccount.Should().NotBeNull();
            dbAccount!.Balance.Should().Be(500m);
        }

        [Fact]
        public async Task UpdateAsync_ShouldUpdateBalance()
        {
            // Arrange
            var account = new SavingsAccount
            {
                UserId = "user_789",
                AccountNumber = "ACC-98765",
                Balance = 1000m,
                Type = AccountType.Primary,
                Status = AccountStatus.Active,
                CreatedByAdminId = "admin"
            };
            
            await _dbContext.Savings.AddAsync(account);
            await _dbContext.SaveChangesAsync();

            // Act
            account.Balance = 2000m;
            await _repository.UpdateAsync(account);
            await _dbContext.SaveChangesAsync();

            // Assert
            var dbAccount = await _dbContext.Savings.AsNoTracking().FirstOrDefaultAsync(a => a.Id == account.Id);
            dbAccount.Should().NotBeNull();
            dbAccount!.Balance.Should().Be(2000m);
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
