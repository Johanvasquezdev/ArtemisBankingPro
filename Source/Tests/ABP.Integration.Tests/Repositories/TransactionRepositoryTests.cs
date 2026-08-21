using System;
using System.Threading.Tasks;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ABP.Integration.Tests.Repositories
{
    public class TransactionRepositoryTests : IDisposable
    {
        private readonly ArtemisBankingDbContext _dbContext;
        private readonly TransactionRepository _repository;

        public TransactionRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ArtemisBankingDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new ArtemisBankingDbContext(options);
            _dbContext.Database.EnsureCreated();

            _repository = new TransactionRepository(_dbContext);
        }

        [Fact]
        public async Task AddAsync_ShouldAddTransactionToDatabase()
        {
            // Arrange
            var savings = new SavingsAccount { UserId = "user_123", Balance = 0, CreatedAt = DateTime.UtcNow, AccountNumber = "ACC-1", CreatedByAdminId = "admin" };
            await _dbContext.Savings.AddAsync(savings);
            await _dbContext.SaveChangesAsync();

            var transaction = new Transaction
            {
                Amount = 1000m,
                Type = TransactionType.Credit,
                SavingAccountId = savings.Id,
                Origin = "Deposit",
                TransactionDate = DateTime.UtcNow,
                Description = "Test Deposit", PerformedByUserId = "user"
            };

            // Act
            await _repository.AddAsync(transaction);
            await _dbContext.SaveChangesAsync();

            // Assert
            transaction.Id.Should().BeGreaterThan(0);

            var dbTransaction = await _dbContext.Transactions.FindAsync(transaction.Id);
            dbTransaction.Should().NotBeNull();
            dbTransaction!.Amount.Should().Be(1000m);
        }

        [Fact]
        public async Task GetTodayTransactionsByUserIdCountAsync_ShouldReturnCorrectCount()
        {
            // Arrange
            var cashierId = "cashier_123";
            var otherId = "other_cashier";

            var acc1 = new SavingsAccount { UserId = cashierId, Balance = 0, CreatedAt = DateTime.UtcNow, AccountNumber = "ACC-1", CreatedByAdminId = "admin" };
            var acc2 = new SavingsAccount { UserId = otherId, Balance = 0, CreatedAt = DateTime.UtcNow, AccountNumber = "ACC-2", CreatedByAdminId = "admin" };

            await _dbContext.Savings.AddRangeAsync(acc1, acc2);
            await _dbContext.SaveChangesAsync();

            var transactions = new[]
            {
                new Transaction { Amount = 100, Type = TransactionType.Credit, SavingAccountId = acc1.Id, PerformedByUserId = cashierId, TransactionDate = DateTime.UtcNow },
                new Transaction { Amount = 200, Type = TransactionType.Debit, SavingAccountId = acc1.Id, PerformedByUserId = cashierId, TransactionDate = DateTime.UtcNow },
                new Transaction { Amount = 300, Type = TransactionType.Credit, SavingAccountId = acc2.Id, PerformedByUserId = otherId, TransactionDate = DateTime.UtcNow },
                new Transaction { Amount = 400, Type = TransactionType.Credit, SavingAccountId = acc1.Id, PerformedByUserId = cashierId, TransactionDate = DateTime.UtcNow.AddDays(-1) }
            };

            await _dbContext.Transactions.AddRangeAsync(transactions);
            await _dbContext.SaveChangesAsync();

            // Act
            var count = await _repository.GetTodayTransactionsByUserIdCountAsync(cashierId);

            // Assert
            count.Should().Be(2); // Only today's transactions for cashier_123
        }

        public void Dispose()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Dispose();
        }
    }
}
