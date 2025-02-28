using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repositories.RepositoryImpl;

public class TagRepository : Repository<Tag, int>, ITagRepository
{
    public TagRepository(FunewsManagementContext context, ILogger<Repository<Tag, int>> logger)
            : base(context, logger)
    {
    }

    public override IQueryable<Tag> AddIncludes(IQueryable<Tag> query)
    {
        return query.Include(e => e.NewsArticles);
    }
}