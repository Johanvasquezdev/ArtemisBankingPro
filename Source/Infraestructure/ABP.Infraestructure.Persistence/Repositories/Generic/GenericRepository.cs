using ABP.Core.Domain.Interfaces.IGenerics;
using ABP.Infraestructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories.Generic
{
    public class GenericRepository<Entity>(ArtemisBankingDbContext context) : IGenericRepository<Entity> where Entity : class
    {
        protected readonly ArtemisBankingDbContext _context = context;
        protected readonly DbSet<Entity> _dbSet = context.Set<Entity>();

        /// <summary>Stages an entity. The application unit of work owns the flush.</summary>
        public virtual Task AddAsync(Entity entity)
            => _dbSet.AddAsync(entity).AsTask();

        public virtual Task AddWithoutSaveAsync(Entity entity)
        {
            return _dbSet.AddAsync(entity).AsTask();
        }

        /// <summary>Stages a deletion. The application unit of work owns the flush.</summary>
        public virtual Task DeleteAsync(Entity entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual Task DeleteWithoutSaveAsync(Entity entity)
        {
            _dbSet.Remove(entity);
            return Task.CompletedTask;
        }

        public virtual IQueryable<Entity> GetAll()
        {
            return  _dbSet.AsNoTracking();
        }

        public virtual async Task<IEnumerable<Entity>> GetAllAsync()
        {
            return await _dbSet.AsNoTracking().ToListAsync();
        }

        public virtual async Task<Entity?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        /// <summary>Stages an update. The application unit of work owns the flush.</summary>
        public virtual Task UpdateAsync(Entity entity)
        {
            StageUpdate(entity);
            return Task.CompletedTask;
        }

        public virtual Task UpdateWithoutSaveAsync(Entity entity)
        {
            StageUpdate(entity);
            return Task.CompletedTask;
        }

        private void StageUpdate(Entity entity)
        {
            var incomingEntry = _context.Entry(entity);
            if (incomingEntry.State != EntityState.Detached)
            {
                incomingEntry.State = EntityState.Modified;
                return;
            }

            var primaryKey = _context.Model.FindEntityType(typeof(Entity))?.FindPrimaryKey();
            if (primaryKey is null)
            {
                incomingEntry.State = EntityState.Modified;
                return;
            }

            var trackedEntity = _dbSet.Local.FirstOrDefault(localEntity =>
                primaryKey.Properties.All(property =>
                    Equals(
                        _context.Entry(localEntity).Property(property.Name).CurrentValue,
                        incomingEntry.Property(property.Name).CurrentValue)));

            if (trackedEntity is null)
            {
                incomingEntry.State = EntityState.Modified;
                return;
            }

            var trackedEntry = _context.Entry(trackedEntity);
            trackedEntry.CurrentValues.SetValues(entity);
            trackedEntry.State = EntityState.Modified;
        }
    }
}
