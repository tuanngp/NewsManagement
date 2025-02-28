using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repositories.RepositoryImpl;

public class NewsArticleRepository : Repository<NewsArticle, string>, INewsArticleRepository
{
    public NewsArticleRepository(
        FunewsManagementContext context,
        ILogger<Repository<NewsArticle, string>> logger
    )
        : base(context, logger) { }

    public override IQueryable<NewsArticle> AddIncludes(IQueryable<NewsArticle> query)
    {
        return query
            .Include(e => e.Category)
            .Include(e => e.CreatedBy)
            .Include(e => e.Tags)
            .Include(e => e.CreatedBy);
    }
}
