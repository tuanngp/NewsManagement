using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repositories.RepositoryImpl
{
    public class Repository<TEntity, ID> : IRepository<TEntity, ID>
        where TEntity : class
    {
        private readonly FunewsManagementContext _context;
        private readonly DbSet<TEntity> _dbSet;
        private readonly ILogger<Repository<TEntity, ID>> _logger;
        private readonly string _primaryKeyName;

        public Repository(FunewsManagementContext context, ILogger<Repository<TEntity, ID>> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _dbSet = context.Set<TEntity>();
            _primaryKeyName = GetPrimaryKeyName();
        }

        private string GetPrimaryKeyName()
        {
            var entityType = _context.Model.FindEntityType(typeof(TEntity));
            var key =
                entityType?.FindPrimaryKey()
                ?? throw new InvalidOperationException(
                    $"No primary key found for {typeof(TEntity).Name}."
                );
            return key.Properties[0].Name;
        }

        public virtual async Task<TEntity?> GetByIdAsync(ID id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            try
            {
                return await AddIncludes(_dbSet)
                    .FirstOrDefaultAsync(e => EF.Property<ID>(e, _primaryKeyName).Equals(id));
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error getting entity with id {Id}", id);
                throw;
            }
        }

        public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            try
            {
                var query = AddIncludes(_dbSet);
                return await query.AsNoTracking().ToListAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Error getting all entities of type {EntityType}",
                    typeof(TEntity).Name
                );
                throw;
            }
        }

        public virtual async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            try
            {
                await _dbSet.AddAsync(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Error adding entity of type {EntityType}",
                    typeof(TEntity).Name
                );
                throw;
            }
        }

        public virtual async Task<TEntity> UpdateAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            try
            {
                _dbSet.Update(entity);
                await _context.SaveChangesAsync();
                return entity;
            }
            catch (Exception e)
            {
                _logger.LogError(
                    e,
                    "Error updating entity of type {EntityType}",
                    typeof(TEntity).Name
                );
                throw;
            }
        }

        public virtual async Task DeleteAsync(ID id)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            try
            {
                var entity = await GetByIdAsync(id);
                if (entity == null)
                {
                    throw new KeyNotFoundException($"Entity with id {id} not found.");
                }
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error deleting entity with id {Id}", id);
                throw;
            }
        }

        public virtual IQueryable<TEntity> GetQueryable() => AddIncludes(_dbSet);

        public virtual IQueryable<TEntity> AddIncludes(IQueryable<TEntity> query) => query;
    }
}
