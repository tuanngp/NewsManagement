namespace Repositories
{
    public interface IRepository<TEntity, ID> where TEntity : class
    {
        Task<TEntity?> GetByIdAsync(ID id);
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<TEntity> AddAsync(TEntity entity);
        Task<TEntity> UpdateAsync(TEntity entity);
        Task DeleteAsync(ID id);
        IQueryable<TEntity> GetQueryable();
        IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query);
    }
}
