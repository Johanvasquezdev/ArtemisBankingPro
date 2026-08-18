using ABP.Core.Domain.Interfaces.IGenerics;
using ABP.Infraestructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories.Generic
{
    public class GenericRepository<Entity>(ArtemisBankingDbContext context) : IGenericRepository<Entity> where Entity : class
    {
        protected readonly ArtemisBankingDbContext _context = context;
        protected readonly DbSet<Entity> _dbSet = context.Set<Entity>();

        public virtual async Task AddAsync(Entity entity)
        {
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public virtual Task AddWithoutSaveAsync(Entity entity)
        {
            return _dbSet.AddAsync(entity).AsTask();
        }

        public virtual async Task DeleteAsync(Entity entity)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
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

        public virtual async Task UpdateAsync(Entity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public virtual Task UpdateWithoutSaveAsync(Entity entity)
        {
            _context.Entry(entity).State = EntityState.Modified;
            return Task.CompletedTask;
        }
    }
}
