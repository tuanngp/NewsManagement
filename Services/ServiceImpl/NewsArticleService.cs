using BusinessObjects.Models;
using Microsoft.EntityFrameworkCore;
using Repositories;
using Repositories.RepositoryImpl;

namespace Services.ServiceImpl;

public class NewsArticleService : INewsArticleService
{
    private INewsArticleRepository _newsArticleRepository;

    public NewsArticleService(INewsArticleRepository newsArticleRepository)
    {
        _newsArticleRepository =
            newsArticleRepository ?? throw new ArgumentNullException(nameof(newsArticleRepository));
    }

    public Task<IEnumerable<NewsArticle>> GetAllAsync()
    {
        return _newsArticleRepository.GetAllAsync();
    }

    public Task<NewsArticle?> GetByIdAsync(string id)
    {
        return _newsArticleRepository.GetByIdAsync(id);
    }

    public Task<NewsArticle> AddAsync(NewsArticle entity)
    {
        return _newsArticleRepository.AddAsync(entity);
    }

    public Task<NewsArticle> UpdateAsync(NewsArticle entity)
    {
        return _newsArticleRepository.UpdateAsync(entity);
    }

    public Task DeleteAsync(string id)
    {
        return _newsArticleRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<NewsArticle>?> GetAllActiveNews()
    {
        return await _newsArticleRepository
            .GetQueryable()
            .Where(x => x.Status == ArticleStatus.Published)
            .ToListAsync();
    }
}
