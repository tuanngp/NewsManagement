using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Repositories.RepositoryImpl;

public class SystemAccountRepository: Repository<SystemAccount, short>, ISystemAccountRepository
{
    public SystemAccountRepository(FunewsManagementContext context, ILogger<Repository<SystemAccount, short>> logger)
            : base(context, logger)
    {
    }
    public override IQueryable<SystemAccount> AddIncludes(IQueryable<SystemAccount> query)
    {
        return query.Include(e => e.NewsArticles);
    }
}

