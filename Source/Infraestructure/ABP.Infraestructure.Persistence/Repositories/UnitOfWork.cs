using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class UnitOfWork(ArtemisBankDbContext context) : IUnitOfWork
    {
        private readonly ArtemisBankDbContext _context = context;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => _context.SaveChangesAsync(cancellationToken);

        public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            return new UnitOfWorkTransaction(transaction);
        }
    }

    public class UnitOfWorkTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction = transaction;

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => _transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken = default)
            => _transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
