namespace ABP.Core.Domain.Interfaces.IGenerics
{
    public interface IGenericRepository<Entity> where Entity : class
    {
        Task<Entity?> GetByIdAsync(int id);
        IQueryable<Entity> GetAll();
        Task<IEnumerable<Entity>> GetAllAsync();
        Task AddAsync(Entity entity);
        Task DeleteAsync(Entity entity);
        Task UpdateAsync(Entity entity);
    }
}