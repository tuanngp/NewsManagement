using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repositories.RepositoryImpl;

public class CategoryRepository : Repository<Category, short>, ICategoryRepository
{
    public CategoryRepository(FunewsManagementContext context, ILogger<Repository<Category, short>> logger)
            : base(context, logger)
    {
    }

    public override IQueryable<Category> AddIncludes(IQueryable<Category> query)
    {
        return query.Include(e => e.NewsArticles)
            .Include(e => e.InverseParentCategory)
            .Include(e => e.ParentCategory);
    }
}